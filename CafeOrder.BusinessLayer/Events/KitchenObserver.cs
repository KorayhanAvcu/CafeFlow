using CafeOrder.DataAccessLayer.Repositories.Notifications;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Events;

/// <summary>OBSERVER: mutfak. Yeni siparişte hazırlanacak ürün sayısını, iptalde "durdur" mesajını alır.</summary>
public class KitchenObserver : IOrderObserver
{
    private readonly INotificationRepository _notificationRepository;
    private readonly TimeProvider _time;

    public KitchenObserver(INotificationRepository notificationRepository, TimeProvider time)
    {
        _notificationRepository = notificationRepository;
        _time = time;
    }

    /// <summary>Yeni siparişte mutfağa hazırlanacak toplam ürün adedini bildirir.</summary>
    public async Task OrderPlacedAsync(Order order)
    {
        await SendAsync(order, $"Yeni sipariş {order.OrderNo}: hazırlanacak {order.Details.Sum(d => d.Quantity)} ürün.");
    }

    /// <summary>Sadece sipariş iptal edildiğinde mutfağa "hazırlamayı durdurun" der; diğer durum değişikliklerini yok sayar.</summary>
    public async Task StatusChangedAsync(Order order, OrderStatus previous)
    {
        if (order.Status == OrderStatus.Cancelled)
        {
            await SendAsync(order, $"{order.OrderNo} siparişi iptal edildi. Hazırlamayı durdurun.");
        }
    }

    /// <summary>Bildirimi "eklenecek" işaretler; servis siparişle birlikte tek SaveChanges ile yazar.</summary>
    private async Task SendAsync(Order order, string message)
    {
        Notification notification = new Notification();
        notification.Order = order;   // Id yeni siparişte henüz yok; EF kayıtta bağlar
        notification.Target = NotificationTarget.Kitchen;
        notification.Message = message;
        notification.CreatedAt = _time.GetLocalNow().DateTime;
        await _notificationRepository.AddAsync(notification);   // kayıt, siparişle aynı SaveChanges'te
    }
}
