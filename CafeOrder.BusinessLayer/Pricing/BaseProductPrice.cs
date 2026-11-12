namespace CafeOrder.BusinessLayer.Pricing;

/// <summary>DECORATOR deseni: en içteki kutu, ürünün seçeneksiz fiyatı.</summary>
public class BaseProductPrice : IPriceComponent
{
    private readonly decimal _basePrice;

    public BaseProductPrice(decimal basePrice)
    {
        _basePrice = basePrice;
    }

    public decimal GetPrice()
    {
        return _basePrice;
    }
}
