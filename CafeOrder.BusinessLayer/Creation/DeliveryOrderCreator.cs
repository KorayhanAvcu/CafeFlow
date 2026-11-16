using AutoMapper;
using CafeOrder.BusinessLayer.States;
using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Creation;

/// <summary>Eve teslimat siparişi: müşteri ve adres bilgisi alınır.</summary>
public class DeliveryOrderCreator : OrderCreator
{
    public DeliveryOrderCreator(IMapper mapper, TimeProvider time, IOrderStatusRecorder statusRecorder)
        : base(mapper, time, statusRecorder)
    {
    }

    public override OrderChannel Channel => OrderChannel.Delivery;

    /// <summary>Teslimat siparişi: AutoMapper DTO'dan adresi, adres notunu ve notu kopyalar; müşteri bağlanır. Teslimatta alma saati olmaz.</summary>
    protected override Order CreateForChannel(CreateOrderDto dto, Customer customer, Table? table)
    {
        Order order = Mapper.Map<Order>(dto);
        order.Channel = OrderChannel.Delivery;   // kanal DTO'dan değil, sınıfın kendisinden
        order.Customer = customer;
        order.PickupTime = null;                 // başka kanalın alanı: alınmaz
        return order;
    }
}
