using CafeOrder.BusinessLayer.Services.Notifications;
using CafeOrder.EntityLayer.Enums;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.Areas.Admin.Controllers;

/// <summary>Mutfak, kasa ve yönetici bildirimleri (Observer deseninin yazdıkları).</summary>
[Route("admin/notifications")]
public class NotificationsController : AdminControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>Bildirimler sayfası. Model: ResultNotificationPageDto (filtrelenmiş liste ve hedef başına okunmamış sayıları).</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(NotificationTarget? target, bool unread = false)
    {
        return View(await _notificationService.GetPageAsync(target, unread));
    }

    /// <summary>Bildirimler sayfasındaki "Okundu yap" düğmesi; seçili hedefin okunmamışlarını okundu yapar.</summary>
    [HttpPost("mark-read")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(NotificationTarget? target)
    {
        await _notificationService.MarkAllReadAsync(target);
        return Redirect("/admin/notifications?target=" + target);
    }

    /// <summary>Üst çubuktaki zilin "Tümünü okundu yap" düğmesi. İşlemden sonra bulunduğu sayfaya döner.</summary>
    [HttpPost("read-all")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReadAll(string? returnUrl)
    {
        await _notificationService.MarkAllReadAsync(NotificationTarget.Admin);

        if (returnUrl != null && Url.IsLocalUrl(returnUrl) && returnUrl.StartsWith("/admin"))
        {
            return LocalRedirect(returnUrl);
        }
        return Redirect("/admin");
    }
}
