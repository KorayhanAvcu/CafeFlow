using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;
using Microsoft.Extensions.Logging;

namespace CafeOrder.BusinessLayer.Events;

/// <summary>
/// OBSERVER deseni: olayı bütün dinleyicilere (mutfak, kasa, admin) iletir.
/// Dinleyiciler BusinessServiceRegistration'da kayıtlıdır; DI hepsini liste olarak verir.
/// Dinleyiciler bildirimleri sadece "eklenecek" işaretler; servis siparişle birlikte tek SaveChanges ile yazar.
/// Bir dinleyici hata verirse loglanır, diğerleri yine haber alır.
/// </summary>
public class OrderEventPublisher : IOrderEventPublisher
{
    private readonly IEnumerable<IOrderObserver> _observers;
    private readonly ILogger<OrderEventPublisher> _logger;

    public OrderEventPublisher(IEnumerable<IOrderObserver> observers, ILogger<OrderEventPublisher> logger)
    {
        _observers = observers;
        _logger = logger;
    }

    /// <summary>Yeni sipariş verildi.</summary>
    public async Task OrderPlacedAsync(Order order)
    {
        foreach (IOrderObserver observer in _observers)
        {
            try
            {
                await observer.OrderPlacedAsync(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{OrderNo} siparişi için bildirim yazılamadı", order.OrderNo);
            }
        }
    }

    /// <summary>Siparişin durumu değişti.</summary>
    public async Task StatusChangedAsync(Order order, OrderStatus previous)
    {
        foreach (IOrderObserver observer in _observers)
        {
            try
            {
                await observer.StatusChangedAsync(order, previous);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{OrderNo} siparişi için bildirim yazılamadı", order.OrderNo);
            }
        }
    }
}
