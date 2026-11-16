using CafeOrder.EntityLayer.Entities;

namespace CafeOrder.BusinessLayer.Pricing;

/// <summary>Bir ürünün seçenekleriyle birlikte birim fiyatını hesaplar.</summary>
public interface IUnitPriceCalculator
{
    /// <summary>Taban fiyat + seçilen seçeneklerin farkları.</summary>
    decimal Calculate(Product product, IEnumerable<ProductOption> options);
}

/// <summary>
/// DECORATOR deseniyle birim fiyat: taban fiyatın üstüne her seçenek bir kat ekler.
/// Decorator katmanları burada kuruluyor: kaç kat olacağı ve her katın değeri sepet satırına göre
/// çalışma anında belli olduğu için DI'dan gelemezler. new'ler bu tek sınıfa hapsedildi;
/// OrderPricingService decorator sınıflarını tanımıyor, sadece IUnitPriceCalculator'ı biliyor.
/// Fiyat hesabını tamamen değiştirmek (ör. kampanya) için yeni bir IUnitPriceCalculator yazıp kaydı değiştirmek yeterli.
/// </summary>
public class DecoratorUnitPriceCalculator : IUnitPriceCalculator
{
    /// <summary>Zinciri kurar ve en dıştaki katmana fiyatı sorar: Shot(+20)(Yulaf(+15)(Büyük(+30)(Taban(160)))) = 225.</summary>
    public decimal Calculate(Product product, IEnumerable<ProductOption> options)
    {
        IPriceComponent price = new BaseProductPrice(product.BasePrice);
        foreach (ProductOption option in options)
        {
            price = new OptionPriceDecorator(price, option.PriceDelta);
        }
        return price.GetPrice();
    }
}
