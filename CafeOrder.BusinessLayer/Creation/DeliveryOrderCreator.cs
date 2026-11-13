using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Creation;

/// <summary>Eve teslimat siparişi: müşteri ve adres bilgisi alınır.</summary>
public class DeliveryOrderCreator : OrderCreator
{
    /// <summary>Teslimat siparişi oluşturur: kanalı Teslimat yapar, formdan müşteriyi, adresi ve adres notunu kopyalar. Diğer kanalların alanlarını (alma saati, masa) bilerek almaz.</summary>
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
