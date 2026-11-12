namespace CafeOrder.BusinessLayer.Pricing;

/// <summary>Ücret ayarları. Değerler appsettings.json → "Pricing" bölümünden okunur.</summary>
public class PricingOptions
{
    public decimal DeliveryFee { get; set; } = 30;        // teslimat ücreti
    public decimal FreeDeliveryFrom { get; set; } = 600;  // bu tutardan sonra teslimat bedava
    public decimal MinDeliveryAmount { get; set; } = 100; // teslimat için en az sipariş tutarı
}
