using CafeOrder.DataAccessLayer.Repositories.Notifications;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Events;

/// <summary>OBSERVER: admin. Her yeni siparişi ve her durum değişikliğini görür (paneldeki zil).</summary>
public class AdminObserver : IOrderObserver
{
    private readonly INotificationRepository _notificationRepository;
    private readonly TimeProvider _time;

    public AdminObserver(INotificationRepository notificationRepository, TimeProvider time)
    {
        _notificationRepository = notificationRepository;
        _time = time;
    }

    /// <summary>Yeni siparişte yönetici bildirimi yazar: "Yeni Teslimat siparişi: CF-...". Paneldeki zilde görünür.</summary>
    public async Task OrderPlacedAsync(Order order)
    {
        await SendAsync(order, $"Yeni {order.Channel.ToText()} siparişi: {order.OrderNo}.");
    }

    /// <summary>Her durum değişikliğinde yönetici bildirimi yazar: "Sipariş CF-...: Alındı → Hazırlanıyor."</summary>
    public async Task StatusChangedAsync(Order order, OrderStatus previous)
    {
        await SendAsync(order, $"Sipariş {order.OrderNo}: {previous.ToText()} → {order.Status.ToText()}.");
    }

    /// <summary>Bildirimi "eklenecek" işaretler; servis siparişle birlikte tek SaveChanges ile yazar.</summary>
    private async Task SendAsync(Order order, string message)
    {
        Notification notification = new Notification();
        notification.Order = order;   // Id yeni siparişte henüz yok; EF kayıtta bağlar
        notification.Target = NotificationTarget.Admin;
        notification.Message = message;
        notification.CreatedAt = _time.GetLocalNow().DateTime;
        await _notificationRepository.AddAsync(notification);   // kayıt, siparişle aynı SaveChanges'te
    }
}
