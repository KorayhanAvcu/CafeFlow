namespace CafeOrder.BusinessLayer.Pricing.Fees;

/// <summary>Masadan sipariş: ücret yok.</summary>
public class TableFeeStrategy : IDeliveryFeeStrategy
{
    public decimal Calculate(decimal orderAmount)
    {
        return 0;
    }
}
