using CafeOrder.BusinessLayer.Services.Account;
using CafeOrder.DtoLayer.Dtos.AccountDtos;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.Areas.Admin.Controllers;

/// <summary>
/// Admin girişi, çıkışı ve şifre değiştirme.
/// Sınıf AdminControllerBase'ten türediği için varsayılan olarak kilitlidir.
/// Login sayfası herkese açık olmak zorunda, o yüzden o action'larda [AllowAnonymous] var.
/// Giriş/çıkış çerezle ilgili olduğu için burada (SignInManager); hesap bilgisi ve şifre değiştirme AccountService'te.
/// </summary>
[Route("admin")]
public class AccountController : AdminControllerBase
{
    private readonly SignInManager<AppUser> _signInManager;   // giriş / çıkış (çerez)
    private readonly UserManager<AppUser> _userManager;       // giriş yapan kullanıcının Id'si, rol kontrolü
    private readonly IAccountService _accountService;         // Hesabım ve şifre değiştirme

    public AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, IAccountService accountService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _accountService = accountService;
    }

    /// <summary>
    /// Giriş formu. ?ReturnUrl=... adres çubuğundan LoginUserDto olarak gelir.
    /// Bu bir GET isteği olduğu için [Required] kurallarının ürettiği hatalar silinir (form boş açılırken hata görünmesin).
    /// </summary>
    [AllowAnonymous]
    [HttpGet("login")]
    public IActionResult LoginPage([FromQuery] LoginUserDto input)
    {
        // Zaten giriş yapmış admin formu tekrar görmesin.
        if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole(AppRoles.Admin))
        {
            return LocalRedirect(SafeReturnUrl(input.ReturnUrl));
        }

        // Metodun adı POST'takiyle aynı olamadığı için (aynı parametre tipi) view adı açıkça veriliyor.
        ModelState.Clear();
        return View("Login", input);
    }

    /// <summary>Giriş formunun gönderildiği yer. Form LoginUserDto olarak gelir.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginUserDto input)
    {
        // [Required], [EmailAddress] kuralları sağlanmadıysa formu geri göster.
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        string email = input.Email!.Trim();

        // isPersistent: true  → tarayıcı kapansa da oturum açık kalır (süresi Program.cs'te).
        // lockoutOnFailure: true → 5 yanlış şifreden sonra hesap 5 dakika kilitlenir.
        var result = await _signInManager.PasswordSignInAsync(email, input.Password!, true, true);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByNameAsync(email);
            bool isAdmin = user != null && await _userManager.IsInRoleAsync(user, AppRoles.Admin);
            if (isAdmin)
            {
                return LocalRedirect(SafeReturnUrl(input.ReturnUrl));
            }

            // Şifresi doğru ama Admin rolü olmayan kullanıcı içeri alınmaz.
            await _signInManager.SignOutAsync();
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError("", "Çok fazla hatalı deneme yapıldı. Lütfen 5 dakika bekleyip tekrar deneyin.");
        }
        else
        {
            // Hangisinin yanlış olduğu bilerek söylenmez (e-postanın var olup olmadığı belli olmasın).
            ModelState.AddModelError("", "E-posta veya şifre hatalı.");
        }

        input.Password = null;   // şifre kutuya geri yazılmasın
        return View(input);
    }

    /// <summary>Çıkış. GET değil POST: başka bir sayfa gizlice çıkış yaptıramasın.</summary>
    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Redirect("/admin/login");
    }

    /// <summary>Giriş yapmış ama Admin olmayan kullanıcı buraya gönderilir.</summary>
    [AllowAnonymous]
    [HttpGet("access-denied")]
    public IActionResult AccessDenied()
    {
        return View();
    }

    /// <summary>Hesabım sayfası. Model: ResultAccountDto (AccountService hazırlar). Kullanıcı bulunamazsa giriş sayfasına gönderir.</summary>
    [HttpGet("account")]
    public async Task<IActionResult> Index()
    {
        var account = await _accountService.GetAccountAsync(_userManager.GetUserId(User));
        if (account == null)
        {
            return Challenge();   // giriş sayfasına gönder
        }
        return View(account);
    }

    /// <summary>
    /// Şifre değiştirme formu. Alan adları "Password.CurrentPassword" gibi olduğu için Prefix verilir; form ChangePasswordDto olarak gelir.
    /// Kurallar ve şifre değişikliği AccountService'te. Başarılıysa oturum çerezi yenilenir (eski çerez geçersiz olduğu için).
    /// </summary>
    [HttpPost("account/password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([Bind(Prefix = "Password")] ChangePasswordDto input)
    {
        var result = await _accountService.ChangePasswordAsync(_userManager.GetUserId(User), input);
        if (result == null)
        {
            return Challenge();
        }
        if (!result.Succeeded)
        {
            return View("Index", result);
        }

        // Şifre değişince güvenlik damgası değişir; bu tarayıcının oturumu açık kalsın diye yenilenir.
        var user = await _userManager.GetUserAsync(User);
        if (user != null)
        {
            await _signInManager.RefreshSignInAsync(user);
        }
        // Başarılı: Hesabım sayfası "Şifreniz değiştirildi" mesajıyla doğrudan çizilir (result.Succeeded = true).
        return View("Index", result);
    }

    /// <summary>
    /// Girişten sonra gidilecek adres. Sadece admin paneli içindeki adreslere izin verilir;
    /// böylece bir link, giriş yapan kişiyi başka bir siteye gönderemez.
    /// </summary>
    private string SafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl))
        {
            return "/admin";
        }
        if (!Url.IsLocalUrl(returnUrl))
        {
            return "/admin";
        }
        if (!returnUrl.StartsWith("/admin", StringComparison.OrdinalIgnoreCase))
        {
            return "/admin";
        }
        if (returnUrl.StartsWith("/admin/login", StringComparison.OrdinalIgnoreCase))
        {
            return "/admin";
        }
        return returnUrl;
    }
}
