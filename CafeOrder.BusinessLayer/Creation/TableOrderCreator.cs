using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Creation;

/// <summary>Masadan (QR) sipariş: sadece masa bağlanır, müşteri adı ve adres istenmez.</summary>
public class TableOrderCreator : OrderCreator
{
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
