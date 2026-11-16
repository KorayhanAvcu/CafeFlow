using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Pricing.Fees;

/// <summary>Gel-al: ücret yok.</summary>
public class PickupFeeStrategy : IDeliveryFeeStrategy
{
    public OrderChannel Channel => OrderChannel.Pickup;

    /// <summary>Gel-al siparişinde ücret yoktur, her zaman 0 döndürür.</summary>
    public decimal Calculate(decimal orderAmount)
    {
        return 0;
    }
}
