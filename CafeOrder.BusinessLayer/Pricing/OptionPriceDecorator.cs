namespace CafeOrder.BusinessLayer.Pricing;

/// <summary>
/// DECORATOR deseni: içindeki fiyatı sarar ve seçeneğin farkını ekler.
/// Örnek: Latte 205 → Large +30 → Oat süt +15 = 250
/// </summary>
public class OptionPriceDecorator : IPriceComponent
{
    private readonly IPriceComponent _inner;
    private readonly decimal _extraPrice;

    public OptionPriceDecorator(IPriceComponent inner, decimal extraPrice)
    {
        _inner = inner;
        _extraPrice = extraPrice;
    }

    /// <summary>İçteki bileşenin fiyatını sorar ve bu seçeneğin fiyat farkını üstüne ekler.</summary>
    public decimal GetPrice()
    {
        return _inner.GetPrice() + _extraPrice;
    }
}
