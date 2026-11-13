namespace CafeOrder.BusinessLayer.Pricing;

/// <summary>DECORATOR deseni: fiyatı olan her şeyin ortak arayüzü.</summary>
public interface IPriceComponent
{
    /// <summary>Bu katmana kadar biriken fiyatı döndürür.</summary>
    decimal GetPrice();
}
