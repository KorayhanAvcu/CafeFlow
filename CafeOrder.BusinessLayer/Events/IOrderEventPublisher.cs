using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Events;

/// <summary>Sipariş olaylarını bütün dinleyicilere (IOrderObserver) iletir.</summary>
public interface IOrderEventPublisher
{
    /// <summary>Yeni sipariş verildi.</summary>
    Task OrderPlacedAsync(Order order);
    /// <summary>Siparişin durumu değişti. previous: önceki durum.</summary>
    Task StatusChangedAsync(Order order, OrderStatus previous);
}
