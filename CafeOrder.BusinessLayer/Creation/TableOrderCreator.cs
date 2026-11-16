using AutoMapper;
using CafeOrder.BusinessLayer.States;
using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Creation;

/// <summary>Masadan (QR) sipariş: sadece masa bağlanır, müşteri adı ve adres istenmez.</summary>
public class TableOrderCreator : OrderCreator
{
    public TableOrderCreator(IMapper mapper, TimeProvider time, IOrderStatusRecorder statusRecorder)
        : base(mapper, time, statusRecorder)
    {
    }

    public override OrderChannel Channel => OrderChannel.Table;

    /// <summary>
    /// Masa siparişi: QR koduyla bulunan masanın Id'si bağlanır; müşteri, adres ve alma saati alınmaz.
    /// Masa bulunamadıysa TableId boş kalır ve doğrulama zinciri siparişi durdurur.
    /// </summary>
    protected override Order CreateForChannel(CreateOrderDto dto, Customer customer, Table? table)
    {
        Order order = Mapper.Map<Order>(dto);   // sadece not işimize yarıyor
        order.Channel = OrderChannel.Table;
        order.TableId = table?.Id;
        order.Address = null;                   // başka kanalların alanları: alınmaz
        order.AddressNotes = null;
        order.PickupTime = null;
        return order;
    }
}
