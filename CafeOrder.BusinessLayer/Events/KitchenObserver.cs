using CafeOrder.DataAccessLayer.Repositories.Notifications;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Events;

/// <summary>OBSERVER: mutfak. Yeni siparişte hazırlanacak ürün sayısını, iptalde "durdur" mesajını alır.</summary>
public class KitchenObserver : IOrderObserver
{
    private readonly INotificationRepository _notificationRepository;

    public KitchenObserver(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task OrderPlacedAsync(Order order)
    {
        await SendAsync(order, $"New order {order.OrderNo}: {order.Details.Sum(d => d.Quantity)} item(s) to prepare.");
    }

    public async Task StatusChangedAsync(Order order, OrderStatus previous)
    {
        if (order.Status == OrderStatus.Cancelled)
        {
            await SendAsync(order, $"Order {order.OrderNo} was cancelled. Stop preparing it.");
        }
    }

    /// <summary>Notifications tablosuna bir kayıt ekler.</summary>
    private async Task SendAsync(Order order, string message)
    {
        Notification notification = new Notification();
        notification.OrderId = order.Id;
        notification.Target = NotificationTarget.Kitchen;
        notification.Message = message;
        notification.CreatedAt = DateTime.Now;
        await _notificationRepository.CreateAsync(notification);
    }
}
