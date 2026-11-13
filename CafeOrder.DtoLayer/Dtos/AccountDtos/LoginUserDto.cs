using System.ComponentModel.DataAnnotations;

namespace CafeOrder.DtoLayer.Dtos.AccountDtos;

/// <summary>
/// Admin giriş formundan gelen veri. [Required] ve [EmailAddress] kurallarını
/// ASP.NET form gönderilince kendisi kontrol eder; sonuç ModelState.IsValid ile okunur.
/// </summary>
public class LoginUserDto
{
    [Required(ErrorMessage = "Lütfen e-posta adresinizi girin.")]
    [EmailAddress(ErrorMessage = "Lütfen geçerli bir e-posta adresi girin.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Lütfen şifrenizi girin.")]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    /// <summary>Giriş sayfasına yönlendirilmeden önce açılmak istenen admin sayfası.</summary>
    public string? ReturnUrl { get; set; }
}
