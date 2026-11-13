using System.ComponentModel.DataAnnotations;

namespace CafeOrder.DtoLayer.Dtos.AccountDtos;

/// <summary>Hesabım sayfasındaki şifre değiştirme formu.</summary>
public class ChangePasswordDto
{
    [Required(ErrorMessage = "Lütfen mevcut şifrenizi girin.")]
    [DataType(DataType.Password)]
    public string? CurrentPassword { get; set; }

    [Required(ErrorMessage = "Lütfen yeni şifreyi girin.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Yeni şifre en az 8 karakter olmalı.")]
    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }

    [Required(ErrorMessage = "Lütfen yeni şifreyi tekrar girin.")]
    [Compare("NewPassword", ErrorMessage = "Yeni şifreler birbiriyle eşleşmiyor.")]
    [DataType(DataType.Password)]
    public string? ConfirmPassword { get; set; }
}
