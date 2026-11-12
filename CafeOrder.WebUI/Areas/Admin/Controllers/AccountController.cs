using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;
using CafeOrder.WebUI.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.Areas.Admin.Controllers;

/// <summary>
/// Admin girişi, çıkışı ve şifre değiştirme.
/// Sınıf AdminControllerBase'ten türediği için varsayılan olarak kilitlidir.
/// Login sayfası herkese açık olmak zorunda, o yüzden o action'larda [AllowAnonymous] var.
/// </summary>
[Route("admin")]
public class AccountController : AdminControllerBase
{
    private readonly SignInManager<AppUser> _signInManager;   // giriş / çıkış (çerez)
    private readonly UserManager<AppUser> _userManager;       // kullanıcı bulma, rol, şifre

    public AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    /// <summary>Giriş formu.</summary>
    [AllowAnonymous]
    [HttpGet("login")]
    public IActionResult Login(string? returnUrl)
    {
        // Zaten giriş yapmış admin formu tekrar görmesin.
        if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole(AppRoles.Admin))
        {
            return LocalRedirect(SafeReturnUrl(returnUrl));
        }

        LoginInput model = new LoginInput();
        model.ReturnUrl = returnUrl;
        return View(model);
    }

    /// <summary>Giriş formunun gönderildiği yer.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginInput input)
    {
        // [Required], [EmailAddress] kuralları sağlanmadıysa formu geri göster.
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        string email = input.Email!.Trim();

        // isPersistent: true  → tarayıcı kapansa da oturum açık kalır (süresi Program.cs'te).
        // lockoutOnFailure: true → 5 yanlış şifreden sonra hesap 5 dakika kilitlenir.
        Microsoft.AspNetCore.Identity.SignInResult result = await _signInManager.PasswordSignInAsync(email, input.Password!, true, true);

        if (result.Succeeded)
        {
            AppUser? user = await _userManager.FindByNameAsync(email);
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
            ModelState.AddModelError("", "Too many wrong attempts. Please wait 5 minutes and try again.");
        }
        else
        {
            // Hangisinin yanlış olduğu bilerek söylenmez (e-postanın var olup olmadığı belli olmasın).
            ModelState.AddModelError("", "Email or password is incorrect.");
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

    /// <summary>Hesabım sayfası.</summary>
    [HttpGet("account")]
    public async Task<IActionResult> Index()
    {
        AppUser? user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();   // giriş sayfasına gönder
        }

        AccountViewModel model = new AccountViewModel();
        model.FullName = user.FullName;
        model.Email = user.Email ?? "";
        return View(model);
    }

    /// <summary>Şifre değiştirme formu. Alan adları "Password.CurrentPassword" gibi olduğu için Prefix verilir.</summary>
    [HttpPost("account/password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([Bind(Prefix = "Password")] ChangePasswordInput input)
    {
        AppUser? user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        AccountViewModel model = new AccountViewModel();
        model.FullName = user.FullName;
        model.Email = user.Email ?? "";

        // Form kuralları ([Required], [Compare] ...) sağlanmadıysa hataları göster.
        if (!ModelState.IsValid)
        {
            foreach (var entry in ModelState.Values)
            {
                foreach (var error in entry.Errors)
                {
                    if (!model.Errors.Contains(error.ErrorMessage))
                    {
                        model.Errors.Add(error.ErrorMessage);
                    }
                }
            }
            return View("Index", model);
        }

        IdentityResult result = await _userManager.ChangePasswordAsync(user, input.CurrentPassword!, input.NewPassword!);
        if (result.Succeeded)
        {
            // Şifre değişince güvenlik damgası değişir; bu tarayıcının oturumu açık kalsın diye yenilenir.
            await _signInManager.RefreshSignInAsync(user);
            Flash("info", "Your password was changed.");
            return Redirect("/admin/account");
        }

        foreach (IdentityError error in result.Errors)
        {
            if (error.Code == "PasswordMismatch")
            {
                model.Errors.Add("Your current password is incorrect.");
            }
            else
            {
                model.Errors.Add(error.Description);
            }
        }
        return View("Index", model);
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
