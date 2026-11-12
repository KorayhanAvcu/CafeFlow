using CafeOrder.DataAccessLayer.Repositories.Notifications;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Events;

/// <summary>OBSERVER: kasa. Yeni siparişin tutarını ve teslim edildiğini öğrenir.</summary>
public class CashierObserver : IOrderObserver
{
    private readonly INotificationRepository _notificationRepository;

    public CashierObserver(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task OrderPlacedAsync(Order order)
    {
        await SendAsync(order, $"Order {order.OrderNo} placed. Total: ₺{order.Total:0.00}.");
    }

    public async Task StatusChangedAsync(Order order, OrderStatus previous)
    {
        if (order.Status == OrderStatus.Delivered)
        {
            await SendAsync(order, $"Order {order.OrderNo} was delivered.");
        }
    }

    /// <summary>Notifications tablosuna bir kayıt ekler.</summary>
    private async Task SendAsync(Order order, string message)
    {
        Notification notification = new Notification();
        notification.OrderId = order.Id;
        notification.Target = NotificationTarget.Cashier;
        notification.Message = message;
        notification.CreatedAt = DateTime.Now;
        await _notificationRepository.CreateAsync(notification);
    }
}
