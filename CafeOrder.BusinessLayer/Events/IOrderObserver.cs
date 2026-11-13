using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Events;

/// <summary>OBSERVER deseni: sipariş olaylarını dinleyen her sınıf bu iki metodu yazar.</summary>
public interface IOrderObserver
{
    /// <summary>Yeni bir sipariş kaydedildiğinde çağrılır.</summary>
    Task OrderPlacedAsync(Order order);
    /// <summary>Siparişin durumu değiştiğinde çağrılır. previous: değişiklikten önceki durum (order.Status artık yeni durumu taşır).</summary>
    Task StatusChangedAsync(Order order, OrderStatus previous);
}
