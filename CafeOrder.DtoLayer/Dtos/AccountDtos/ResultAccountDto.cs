namespace CafeOrder.DtoLayer.Dtos.AccountDtos;

/// <summary>
/// Hesabım sayfasına giden veri: giriş yapan yöneticinin bilgileri, şifre formunun hataları ve şifre değiştirmenin sonucu.
/// </summary>
public class ResultAccountDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new List<string>();

    /// <summary>Şifre değiştirme başarılı mı? (AccountService.ChangePasswordAsync doldurur.)</summary>
    public bool Succeeded { get; set; }
}
