namespace CafeOrder.WebUI.Identity;

/// <summary>
/// "AdminAccount" ayarları: e-posta ve ad appsettings.Development.json içinde, şifre User Secrets içinde (repoya girmez).
/// Veritabanında hiç admin yoksa bu bilgilerle bir tane oluşturulur.
/// Bölüm yoksa (örneğin canlı ortamda) hiçbir hesap oluşturulmaz.
/// </summary>
public class AdminAccountOptions
{
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string FullName { get; set; } = "Kafe Yöneticisi";
}
