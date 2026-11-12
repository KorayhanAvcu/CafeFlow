namespace CafeOrder.BusinessLayer.Pricing.Fees;

/// <summary>STRATEGY deseni: her sipariş türünün kendi ücret kuralı var.</summary>
public interface IDeliveryFeeStrategy
{
    decimal Calculate(decimal orderAmount);
}
