using System.ComponentModel.DataAnnotations;
using CafeOrder.DtoLayer.Dtos.AccountDtos;
using CafeOrder.EntityLayer.Entities;
using Microsoft.AspNetCore.Identity;

namespace CafeOrder.BusinessLayer.Services.Account;

/// <summary>
/// Yöneticinin hesap bilgileri ve şifre değiştirme. Kullanıcılar Identity'nin UserManager'ı ile okunur ve güncellenir.
/// (Giriş ve çıkış çerezle ilgili olduğu için web katmanında, AccountController'da kalır.)
/// </summary>
public class AccountService : IAccountService
{
    private readonly UserManager<AppUser> _userManager;

    public AccountService(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    /// <summary>Kullanıcıyı Id ile bulur ve Hesabım sayfası için ad ile e-postayı DTO olarak döndürür. Yoksa null.</summary>
    public async Task<ResultAccountDto?> GetAccountAsync(string? userId)
    {
        var user = await FindUserAsync(userId);
        if (user == null)
        {
            return null;
        }
        return CreateAccountDto(user);
    }

    /// <summary>
    /// Şifre değiştirme: önce formun kurallarını ([Required], [StringLength], [Compare]) kontrol eder,
    /// sonra Identity ile mevcut şifreyi doğrulayıp yenisini kaydeder. Hatalar Türkçe olarak Errors listesine yazılır.
    /// </summary>
    public async Task<ResultAccountDto?> ChangePasswordAsync(string? userId, ChangePasswordDto dto)
    {
        var user = await FindUserAsync(userId);
        if (user == null)
        {
            return null;
        }
        ResultAccountDto result = CreateAccountDto(user);

        // 1) Form kuralları (DTO'daki attribute'lar)
        List<ValidationResult> validation = new List<ValidationResult>();
        if (!Validator.TryValidateObject(dto, new ValidationContext(dto), validation, true))
        {
            foreach (ValidationResult error in validation)
            {
                if (error.ErrorMessage != null && !result.Errors.Contains(error.ErrorMessage))
                {
                    result.Errors.Add(error.ErrorMessage);
                }
            }
            return result;
        }

        // 2) Identity: mevcut şifre doğru mu, yeni şifre kurallara uyuyor mu?
        var identityResult = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword!, dto.NewPassword!);
        if (identityResult.Succeeded)
        {
            result.Succeeded = true;
            return result;
        }
        foreach (IdentityError error in identityResult.Errors)
        {
            result.Errors.Add(error.Code == "PasswordMismatch" ? "Mevcut şifreniz hatalı." : error.Description);
        }
        return result;
    }

    /// <summary>Id boşsa null, değilse kullanıcıyı bulur.</summary>
    private async Task<AppUser?> FindUserAsync(string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }
        return await _userManager.FindByIdAsync(userId);
    }

    /// <summary>Kullanıcının adı ve e-postasıyla Hesabım DTO'sunu oluşturur.</summary>
    private static ResultAccountDto CreateAccountDto(AppUser user)
    {
        ResultAccountDto dto = new ResultAccountDto();
        dto.FullName = user.FullName;
        dto.Email = user.Email ?? "";
        return dto;
    }
}
