using CafeOrder.DataAccessLayer.Repositories.Notifications;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Events;

/// <summary>OBSERVER: admin. Her yeni siparişi ve her durum değişikliğini görür (paneldeki zil).</summary>
public class AdminObserver : IOrderObserver
{
    private readonly INotificationRepository _notificationRepository;

    public AdminObserver(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task OrderPlacedAsync(Order order)
    {
        await SendAsync(order, $"New {order.Channel} order {order.OrderNo}.");
    }

    public async Task StatusChangedAsync(Order order, OrderStatus previous)
    {
        await SendAsync(order, $"Order {order.OrderNo}: {previous} → {order.Status}.");
    }

    /// <summary>Notifications tablosuna bir kayıt ekler.</summary>
    private async Task SendAsync(Order order, string message)
    {
        Notification notification = new Notification();
        notification.OrderId = order.Id;
        notification.Target = NotificationTarget.Admin;
        notification.Message = message;
        notification.CreatedAt = DateTime.Now;
        await _notificationRepository.CreateAsync(notification);
    }
}
