using CafeOrder.BusinessLayer.Exceptions;
using CafeOrder.BusinessLayer.Pricing;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Validation;

/// <summary>Halka 4: eve teslimatta sipariş tutarı en az 100 ₺ olmalı.</summary>
public class OrderAmountHandler : OrderValidationHandler
{
    private readonly PricingOptions _options;

    public OrderAmountHandler(PricingOptions options)
    {
        _options = options;
    }

    /// <summary>Halka 4: teslimat siparişinde ara toplamın en az sipariş tutarına (100 ₺) ulaştığını kontrol eder.</summary>
    protected override void Check(Order order)
    {
        if (order.Channel == OrderChannel.Delivery && order.Subtotal < _options.MinDeliveryAmount)
        {
            throw new BusinessException("Teslimat için en az sipariş tutarı " + _options.MinDeliveryAmount.ToString("0") + " ₺.");
        }
    }
}
