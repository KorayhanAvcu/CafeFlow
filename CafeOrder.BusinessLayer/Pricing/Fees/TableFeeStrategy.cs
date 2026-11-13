namespace CafeOrder.BusinessLayer.Pricing.Fees;

/// <summary>Masadan sipariş: ücret yok.</summary>
public class TableFeeStrategy : IDeliveryFeeStrategy
{
    /// <summary>Masa siparişinde ücret yoktur, her zaman 0 döndürür.</summary>
    public decimal Calculate(decimal orderAmount)
    {
        return 0;
    }
}
