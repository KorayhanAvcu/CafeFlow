using CafeOrder.EntityLayer.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.Areas.Admin.Controllers;

/// <summary>
/// Bütün admin controller'ları bundan türer.
/// [Authorize] burada olduğu için yeni bir admin sayfası eklendiğinde kilidi unutmak mümkün değil:
/// giriş yapmamış kişi /admin/login sayfasına gönderilir.
/// </summary>
[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public abstract class AdminControllerBase : Controller
{
    /// <summary>
    /// Redirect'ten sonraki sayfada bir kez gösterilecek mesaj (TempData bir sonraki isteğe kadar yaşar).
    /// type: "info" (yeşil) veya "warn" (sarı). Mesajı _Flash.cshtml gösterir.
    /// </summary>
    protected void Flash(string type, string? message)
    {
        TempData["FlashType"] = type;
        TempData["Flash"] = message;
    }
}
