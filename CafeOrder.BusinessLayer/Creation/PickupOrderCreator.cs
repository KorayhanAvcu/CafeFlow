using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Creation;

/// <summary>Gel-al siparişi: müşteri bilgisi ve alma saati alınır.</summary>
public class PickupOrderCreator : OrderCreator
{
    protected override Order CreateForChannel(Order form, Table? table)
    {
        Order order = new Order();
        order.Channel = OrderChannel.Pickup;
        order.Customer = form.Customer;
        order.PickupTime = form.PickupTime;
        return order;
    }
}
