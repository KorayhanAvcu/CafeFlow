using System.ComponentModel.DataAnnotations;

namespace CafeOrder.BusinessLayer.Pricing;

/// <summary>
/// Ücret ayarları. Değerler appsettings.json → "Pricing" bölümünden okunur (Options pattern).
/// Sınıflar IOptions&lt;PricingOptions&gt; alır. Değerler geçersizse uygulama açılışta hata verir (ValidateOnStart).
/// </summary>
public class PricingOptions
{
    public const string SectionName = "Pricing";

    /// <summary>Teslimat ücreti.</summary>
    [Range(0, 10000)]
    public decimal DeliveryFee { get; set; } = 30;

    /// <summary>Bu tutardan sonra teslimat bedava.</summary>
    [Range(0, 100000)]
    public decimal FreeDeliveryFrom { get; set; } = 600;

    /// <summary>Teslimat için en az sipariş tutarı.</summary>
    [Range(0, 100000)]
    public decimal MinDeliveryAmount { get; set; } = 100;
}
