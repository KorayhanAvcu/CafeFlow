using System.Globalization;
using CafeOrder.DataAccessLayer.Repositories.Notifications;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Events;

/// <summary>OBSERVER: kasa. Yeni siparişin tutarını ve teslim edildiğini öğrenir.</summary>
public class CashierObserver : IOrderObserver
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    private readonly INotificationRepository _notificationRepository;
    private readonly TimeProvider _time;

    public CashierObserver(INotificationRepository notificationRepository, TimeProvider time)
    {
        _notificationRepository = notificationRepository;
        _time = time;
    }

    /// <summary>Yeni siparişte kasaya siparişin toplam tutarını bildirir.</summary>
    public async Task OrderPlacedAsync(Order order)
    {
        await SendAsync(order, $"{order.OrderNo} siparişi verildi. Toplam: ₺{order.Total.ToString("N2", Turkish)}.");
    }

    /// <summary>Sadece sipariş Teslim Edildi olduğunda kasaya haber verir; diğer durum değişikliklerini yok sayar.</summary>
    public async Task StatusChangedAsync(Order order, OrderStatus previous)
    {
        if (order.Status == OrderStatus.Delivered)
        {
            await SendAsync(order, $"{order.OrderNo} siparişi teslim edildi.");
        }
    }

    /// <summary>Bildirimi "eklenecek" işaretler; servis siparişle birlikte tek SaveChanges ile yazar.</summary>
    private async Task SendAsync(Order order, string message)
    {
        Notification notification = new Notification();
        notification.Order = order;   // Id yeni siparişte henüz yok; EF kayıtta bağlar
        notification.Target = NotificationTarget.Cashier;
        notification.Message = message;
        notification.CreatedAt = _time.GetLocalNow().DateTime;
        await _notificationRepository.AddAsync(notification);   // kayıt, siparişle aynı SaveChanges'te
    }
}
