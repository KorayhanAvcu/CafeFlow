using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Pricing.Fees;

/// <summary>
/// STRATEGY deseni: her sipariş türünün kendi ücret kuralı var.
/// Bütün stratejiler DI'a kayıtlıdır; OrderPricingService hepsini IEnumerable olarak alır ve
/// Channel'a göre seçer. Yeni bir kanal = yeni bir sınıf + bir kayıt satırı; servis değişmez.
/// </summary>
public interface IDeliveryFeeStrategy
{
    /// <summary>Bu stratejinin geçerli olduğu sipariş türü.</summary>
    OrderChannel Channel { get; }

    /// <summary>Ara toplama göre bu kanalın ücretini hesaplar.</summary>
    decimal Calculate(decimal orderAmount);
}
