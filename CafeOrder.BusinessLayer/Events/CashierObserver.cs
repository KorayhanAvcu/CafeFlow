using System.Globalization;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Events;

/// <summary>OBSERVER: kasa. Yeni siparişin tutarını ve teslim edildiğini öğrenir.</summary>
public class CashierObserver : IOrderObserver
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    private readonly INotificationWriter _writer;

    public CashierObserver(INotificationWriter writer)
    {
        _writer = writer;
    }

    /// <summary>Yeni siparişte kasaya siparişin toplam tutarını bildirir.</summary>
    public async Task OrderPlacedAsync(Order order)
    {
        await _writer.AddAsync(order, NotificationTarget.Cashier,
            $"{order.OrderNo} siparişi verildi. Toplam: ₺{order.Total.ToString("N2", Turkish)}.");
    }

    /// <summary>Sadece sipariş Teslim Edildi olduğunda kasaya haber verir; diğer durum değişikliklerini yok sayar.</summary>
    public async Task StatusChangedAsync(Order order, OrderStatus previous)
    {
        if (order.Status == OrderStatus.Delivered)
        {
            await _writer.AddAsync(order, NotificationTarget.Cashier, $"{order.OrderNo} siparişi teslim edildi.");
        }
    }
}
