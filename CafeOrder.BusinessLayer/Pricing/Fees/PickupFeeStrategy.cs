namespace CafeOrder.BusinessLayer.Pricing.Fees;

/// <summary>Gel-al: ücret yok.</summary>
public class PickupFeeStrategy : IDeliveryFeeStrategy
{
    public decimal Calculate(decimal orderAmount)
    {
        return 0;
    }
}
