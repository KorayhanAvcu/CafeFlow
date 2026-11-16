using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Creation;

/// <summary>Masadan (QR) sipariş: sadece masa bağlanır, müşteri adı ve adres istenmez.</summary>
public class TableOrderCreator : OrderCreator
{
    public TableOrderCreator(TimeProvider time) : base(time)
    {
    }

    public override OrderChannel Channel => OrderChannel.Table;

    /// <summary>Masa siparişi oluşturur: kanalı Masa yapar ve QR koduyla bulunan masanın Id'sini bağlar. Müşteri ve adres bilgisi almaz; masa bulunamadıysa TableId boş kalır ve doğrulama zinciri siparişi durdurur.</summary>
    protected override Order CreateForChannel(Order form, Table? table)
    {
        Order order = new Order();
        order.Channel = OrderChannel.Table;
        if (table != null)
        {
            order.TableId = table.Id;
        }
        return order;
    }
}
