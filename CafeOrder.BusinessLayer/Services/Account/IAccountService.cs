using CafeOrder.DtoLayer.Dtos.AccountDtos;

namespace CafeOrder.BusinessLayer.Services.Account;

/// <summary>Giriş yapmış yöneticinin hesap işlemleri (Hesabım sayfası).</summary>
public interface IAccountService
{
    /// <summary>Hesabım sayfasının verisi: yöneticinin adı ve e-postası. Kullanıcı bulunamazsa null.</summary>
    Task<ResultAccountDto?> GetAccountAsync(string? userId);

    /// <summary>Şifreyi değiştirir. Sonuçta Succeeded = true ya da Errors listesi dolu döner. Kullanıcı bulunamazsa null.</summary>
    Task<ResultAccountDto?> ChangePasswordAsync(string? userId, ChangePasswordDto dto);
}
