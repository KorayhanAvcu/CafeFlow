using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Creation;

/// <summary>Eve teslimat siparişi: müşteri ve adres bilgisi alınır.</summary>
public class DeliveryOrderCreator : OrderCreator
{
    protected override Order CreateForChannel(Order form, Table? table)
    {
        Order order = new Order();
        order.Channel = OrderChannel.Delivery;
        order.Customer = form.Customer;
        order.Address = form.Address;
        order.AddressNotes = form.AddressNotes;
        return order;
    }
}
