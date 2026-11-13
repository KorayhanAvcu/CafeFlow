using Microsoft.AspNetCore.Identity;

namespace CafeOrder.WebUI.Identity;

/// <summary>
/// Identity'nin hata mesajları varsayılan olarak İngilizcedir (örneğin şifre kuralları).
/// Bu sınıf panelde görünebilecek mesajları Türkçe verir. Program.cs'te AddErrorDescriber ile kayıtlı.
/// </summary>
public class TurkishIdentityErrorDescriber : IdentityErrorDescriber
{
    /// <summary>Tanımsız bir Identity hatası. Mesajı Türkçe döndürür.</summary>
    public override IdentityError DefaultError()
        => new IdentityError { Code = nameof(DefaultError), Description = "Bilinmeyen bir hata oluştu." };

    /// <summary>Girilen şifre yanlış. Mesajı Türkçe döndürür.</summary>
    public override IdentityError PasswordMismatch()
        => new IdentityError { Code = nameof(PasswordMismatch), Description = "Şifre hatalı." };

    /// <summary>Şifre gereken uzunluktan kısa. Mesajı Türkçe döndürür.</summary>
    public override IdentityError PasswordTooShort(int length)
        => new IdentityError { Code = nameof(PasswordTooShort), Description = $"Şifre en az {length} karakter olmalı." };

    /// <summary>Şifrede rakam yok. Mesajı Türkçe döndürür.</summary>
    public override IdentityError PasswordRequiresDigit()
        => new IdentityError { Code = nameof(PasswordRequiresDigit), Description = "Şifre en az bir rakam içermeli." };

    /// <summary>Şifrede küçük harf yok. Mesajı Türkçe döndürür.</summary>
    public override IdentityError PasswordRequiresLower()
        => new IdentityError { Code = nameof(PasswordRequiresLower), Description = "Şifre en az bir küçük harf içermeli." };

    /// <summary>Şifrede büyük harf yok. Mesajı Türkçe döndürür.</summary>
    public override IdentityError PasswordRequiresUpper()
        => new IdentityError { Code = nameof(PasswordRequiresUpper), Description = "Şifre en az bir büyük harf içermeli." };

    /// <summary>Şifrede sembol yok. Mesajı Türkçe döndürür.</summary>
    public override IdentityError PasswordRequiresNonAlphanumeric()
        => new IdentityError { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Şifre en az bir sembol (! ? * gibi) içermeli." };

    /// <summary>Şifrede yeterince farklı karakter yok. Mesajı Türkçe döndürür.</summary>
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars)
        => new IdentityError { Code = nameof(PasswordRequiresUniqueChars), Description = $"Şifre en az {uniqueChars} farklı karakter içermeli." };

    /// <summary>Kullanıcı adı zaten alınmış. Mesajı Türkçe döndürür.</summary>
    public override IdentityError DuplicateUserName(string userName)
        => new IdentityError { Code = nameof(DuplicateUserName), Description = $"{userName} kullanıcı adı zaten kullanılıyor." };

    /// <summary>E-posta zaten kullanılıyor. Mesajı Türkçe döndürür.</summary>
    public override IdentityError DuplicateEmail(string email)
        => new IdentityError { Code = nameof(DuplicateEmail), Description = $"{email} e-posta adresi zaten kullanılıyor." };

    /// <summary>E-posta biçimi geçersiz. Mesajı Türkçe döndürür.</summary>
    public override IdentityError InvalidEmail(string? email)
        => new IdentityError { Code = nameof(InvalidEmail), Description = $"{email} geçerli bir e-posta adresi değil." };

    /// <summary>Kullanıcı adı geçersiz. Mesajı Türkçe döndürür.</summary>
    public override IdentityError InvalidUserName(string? userName)
        => new IdentityError { Code = nameof(InvalidUserName), Description = $"{userName} geçerli bir kullanıcı adı değil." };

    /// <summary>Şifre sıfırlama gibi işlemlerdeki anahtar geçersiz. Mesajı Türkçe döndürür.</summary>
    public override IdentityError InvalidToken()
        => new IdentityError { Code = nameof(InvalidToken), Description = "Geçersiz işlem anahtarı." };

    /// <summary>Kayıt aynı anda başka biri tarafından değiştirildi. Mesajı Türkçe döndürür.</summary>
    public override IdentityError ConcurrencyFailure()
        => new IdentityError { Code = nameof(ConcurrencyFailure), Description = "Kayıt başka biri tarafından değiştirildi. Sayfayı yenileyip tekrar deneyin." };

    /// <summary>Rol adı zaten var. Mesajı Türkçe döndürür.</summary>
    public override IdentityError DuplicateRoleName(string role)
        => new IdentityError { Code = nameof(DuplicateRoleName), Description = $"{role} rolü zaten var." };

    /// <summary>Kullanıcı zaten bu rolde. Mesajı Türkçe döndürür.</summary>
    public override IdentityError UserAlreadyInRole(string role)
        => new IdentityError { Code = nameof(UserAlreadyInRole), Description = $"Kullanıcı zaten {role} rolünde." };

    /// <summary>Kullanıcı için kilitleme kapalı. Mesajı Türkçe döndürür.</summary>
    public override IdentityError UserLockoutNotEnabled()
        => new IdentityError { Code = nameof(UserLockoutNotEnabled), Description = "Bu kullanıcı için kilitleme açık değil." };
}
