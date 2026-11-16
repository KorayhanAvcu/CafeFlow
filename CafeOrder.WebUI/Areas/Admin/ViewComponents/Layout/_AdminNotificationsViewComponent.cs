using CafeOrder.BusinessLayer.Services.Notifications;
using CafeOrder.DtoLayer.Dtos.NotificationDtos;
using CafeOrder.EntityLayer.Enums;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.Areas.Admin.ViewComponents.Layout;

/// <summary>
/// Üst çubuktaki zil: son 6 admin bildirimi ve okunmamış sayısı.
/// _AdminLayout içinde çağrılır, yani her admin sayfasında çalışır.
/// View: Areas/Admin/Views/Shared/Components/_AdminNotifications/Default.cshtml
/// </summary>
public class _AdminNotificationsViewComponent : ViewComponent
{
    private readonly INotificationService _notificationService;

    public _AdminNotificationsViewComponent(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>Üst çubuktaki zili çizer: okunmamış yönetici bildirimi sayısı ve son 6 bildirim.</summary>
    public async Task<IViewComponentResult> InvokeAsync()
    {
        ViewBag.UnreadCount = await _notificationService.CountUnreadAsync(NotificationTarget.Admin);
        var latest = await _notificationService.GetLatestAsync(NotificationTarget.Admin, 6);
        return View(latest);
    }
}
