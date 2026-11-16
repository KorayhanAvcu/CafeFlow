using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Events;

/// <summary>OBSERVER: admin. Her yeni siparişi ve her durum değişikliğini görür (paneldeki zil).</summary>
public class AdminObserver : IOrderObserver
{
    private readonly INotificationWriter _writer;

    public AdminObserver(INotificationWriter writer)
    {
        _writer = writer;
    }

    /// <summary>Yeni siparişte yönetici bildirimi yazar: "Yeni Teslimat siparişi: CF-...". Paneldeki zilde görünür.</summary>
    public async Task OrderPlacedAsync(Order order)
    {
        await _writer.AddAsync(order, NotificationTarget.Admin, $"Yeni {order.Channel.ToText()} siparişi: {order.OrderNo}.");
    }

    /// <summary>Her durum değişikliğinde yönetici bildirimi yazar: "Sipariş CF-...: Alındı → Hazırlanıyor."</summary>
    public async Task StatusChangedAsync(Order order, OrderStatus previous)
    {
        await _writer.AddAsync(order, NotificationTarget.Admin,
            $"Sipariş {order.OrderNo}: {previous.ToText()} → {order.Status.ToText()}.");
    }
}
