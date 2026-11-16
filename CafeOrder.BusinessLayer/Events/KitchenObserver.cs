using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Events;

/// <summary>OBSERVER: mutfak. Yeni siparişte hazırlanacak ürün sayısını, iptalde "durdur" mesajını alır.</summary>
public class KitchenObserver : IOrderObserver
{
    private readonly INotificationWriter _writer;

    public KitchenObserver(INotificationWriter writer)
    {
        _writer = writer;
    }

    /// <summary>Yeni siparişte mutfağa hazırlanacak toplam ürün adedini bildirir.</summary>
    public async Task OrderPlacedAsync(Order order)
    {
        await _writer.AddAsync(order, NotificationTarget.Kitchen,
            $"Yeni sipariş {order.OrderNo}: hazırlanacak {order.Details.Sum(d => d.Quantity)} ürün.");
    }

    /// <summary>Sadece sipariş iptal edildiğinde mutfağa "hazırlamayı durdurun" der; diğer durum değişikliklerini yok sayar.</summary>
    public async Task StatusChangedAsync(Order order, OrderStatus previous)
    {
        if (order.Status == OrderStatus.Cancelled)
        {
            await _writer.AddAsync(order, NotificationTarget.Kitchen,
                $"{order.OrderNo} siparişi iptal edildi. Hazırlamayı durdurun.");
        }
    }
}
