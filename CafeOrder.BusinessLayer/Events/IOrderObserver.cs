using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Events;

/// <summary>OBSERVER deseni: sipariş olaylarını dinleyen her sınıf bu iki metodu yazar.</summary>
public interface IOrderObserver
{
    Task OrderPlacedAsync(Order order);
    Task StatusChangedAsync(Order order, OrderStatus previous);
}
