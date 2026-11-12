using System.ComponentModel.DataAnnotations;

namespace CafeOrder.WebUI.Areas.Admin.Models;

/// <summary>Giriş formu.</summary>
public class LoginInput
{
    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    /// <summary>Giriş sayfasına yönlendirilmeden önce açılmak istenen admin sayfası.</summary>
    public string? ReturnUrl { get; set; }
}

/// <summary>Şifre değiştirme formu.</summary>
public class ChangePasswordInput
{
    [Required(ErrorMessage = "Please enter your current password.")]
    [DataType(DataType.Password)]
    public string? CurrentPassword { get; set; }

    [Required(ErrorMessage = "Please enter a new password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "The new password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }

    [Required(ErrorMessage = "Please repeat the new password.")]
    [Compare("NewPassword", ErrorMessage = "The two new passwords do not match.")]
    [DataType(DataType.Password)]
    public string? ConfirmPassword { get; set; }
}

/// <summary>Hesabım sayfasının modeli.</summary>
public class AccountViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public ChangePasswordInput Password { get; set; } = new ChangePasswordInput();
    public List<string> Errors { get; set; } = new List<string>();
}
