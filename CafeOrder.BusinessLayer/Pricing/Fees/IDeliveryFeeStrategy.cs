namespace CafeOrder.BusinessLayer.Pricing.Fees;

/// <summary>STRATEGY deseni: her sipariş türünün kendi ücret kuralı var.</summary>
public interface IDeliveryFeeStrategy
{
    /// <summary>Ara toplama göre bu kanalın ücretini hesaplar.</summary>
    decimal Calculate(decimal orderAmount);
}
