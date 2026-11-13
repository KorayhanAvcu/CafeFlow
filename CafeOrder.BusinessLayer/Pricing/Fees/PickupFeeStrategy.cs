namespace CafeOrder.BusinessLayer.Pricing.Fees;

/// <summary>Gel-al: ücret yok.</summary>
public class PickupFeeStrategy : IDeliveryFeeStrategy
{
    /// <summary>Gel-al siparişinde ücret yoktur, her zaman 0 döndürür.</summary>
    public decimal Calculate(decimal orderAmount)
    {
        return 0;
    }
}
