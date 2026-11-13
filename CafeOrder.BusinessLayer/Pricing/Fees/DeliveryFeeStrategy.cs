namespace CafeOrder.BusinessLayer.Pricing.Fees;

/// <summary>Eve teslimat: 30 ₺; sipariş 600 ₺ ve üstüyse bedava (rakamlar appsettings.json'dan).</summary>
public class DeliveryFeeStrategy : IDeliveryFeeStrategy
{
    private readonly PricingOptions _options;

    public DeliveryFeeStrategy(PricingOptions options)
    {
        _options = options;
    }

    /// <summary>Ara toplam ücretsiz teslimat sınırının (600 ₺) üstündeyse 0, değilse teslimat ücretini (30 ₺) döndürür. Rakamlar appsettings.json'dan gelir.</summary>
    public decimal Calculate(decimal orderAmount)
    {
        if (orderAmount >= _options.FreeDeliveryFrom)
        {
            return 0;
        }
        return _options.DeliveryFee;
    }
}
