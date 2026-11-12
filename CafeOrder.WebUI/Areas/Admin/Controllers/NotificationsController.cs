using CafeOrder.BusinessLayer.Services.Notifications;
using CafeOrder.EntityLayer.Enums;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.Areas.Admin.Controllers;

/// <summary>Mutfak, kasa ve admin bildirimleri (Observer deseninin yazdıkları).</summary>
[Route("admin/notifications")]
public class NotificationsController : AdminControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(NotificationTarget? target, bool unread = false)
    {
        ViewBag.Target = target;
        ViewBag.Unread = unread;

        // Üstteki kartlar: okunmamış sayıları
        Dictionary<string, int> unreadCounts = new Dictionary<string, int>();
        unreadCounts["All"] = await _notificationService.CountUnreadAsync(null);
        unreadCounts["Kitchen"] = await _notificationService.CountUnreadAsync(NotificationTarget.Kitchen);
        unreadCounts["Cashier"] = await _notificationService.CountUnreadAsync(NotificationTarget.Cashier);
        unreadCounts["Admin"] = await _notificationService.CountUnreadAsync(NotificationTarget.Admin);
        ViewBag.UnreadCounts = unreadCounts;

        return View(await _notificationService.GetAllAsync(target, unread));
    }

    /// <summary>Bildirimler sayfasındaki "Mark as read" düğmesi.</summary>
    [HttpPost("mark-read")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(NotificationTarget? target)
    {
        await _notificationService.MarkAllReadAsync(target);
        Flash("info", "Notifications are marked as read.");
        return RedirectToAction("Index", new { target = target });
    }

    /// <summary>Üst çubuktaki zilin "Mark all as read" düğmesi. İşlemden sonra bulunduğu sayfaya döner.</summary>
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
