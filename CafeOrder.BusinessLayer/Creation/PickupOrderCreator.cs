using AutoMapper;
using CafeOrder.BusinessLayer.States;
using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Creation;

/// <summary>Gel-al siparişi: müşteri bilgisi ve alma saati alınır.</summary>
public class PickupOrderCreator : OrderCreator
{
    public PickupOrderCreator(IMapper mapper, TimeProvider time, IOrderStatusRecorder statusRecorder)
        : base(mapper, time, statusRecorder)
    {
    }

    public override OrderChannel Channel => OrderChannel.Pickup;

    /// <summary>Gel-al siparişi: AutoMapper DTO'dan alma saatini ve notu kopyalar; müşteri bağlanır. Adres alınmaz.</summary>
    protected override Order CreateForChannel(CreateOrderDto dto, Customer customer, Table? table)
    {
        Order order = Mapper.Map<Order>(dto);
        order.Channel = OrderChannel.Pickup;
        order.Customer = customer;
        order.Address = null;          // başka kanalın alanları: alınmaz
        order.AddressNotes = null;
        return order;
    }
}
