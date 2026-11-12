namespace CafeOrder.BusinessLayer.Pricing.Fees;

/// <summary>Eve teslimat: 30 ₺; sipariş 600 ₺ ve üstüyse bedava (rakamlar appsettings.json'dan).</summary>
public class DeliveryFeeStrategy : IDeliveryFeeStrategy
{
    private readonly PricingOptions _options;

    public DeliveryFeeStrategy(PricingOptions options)
    {
        _options = options;
    }

    public decimal Calculate(decimal orderAmount)
    {
        if (orderAmount >= _options.FreeDeliveryFrom)
        {
            return 0;
        }
        return _options.DeliveryFee;
    }
}
