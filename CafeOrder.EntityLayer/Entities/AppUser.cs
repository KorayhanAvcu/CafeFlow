using Microsoft.AspNetCore.Identity;

namespace CafeOrder.EntityLayer.Entities;

/// <summary>
/// Admin panele giriş yapan kişi. Kullanıcı adı, şifre özeti (hash), e-posta, kilitlenme gibi
/// alanlar hazır olarak IdentityUser'dan gelir; biz sadece ekranda görünen adı ekliyoruz.
/// </summary>
public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}
