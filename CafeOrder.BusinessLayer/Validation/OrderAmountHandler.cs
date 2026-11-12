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

    protected override void Check(Order order)
    {
        if (order.Channel == OrderChannel.Delivery && order.Subtotal < _options.MinDeliveryAmount)
        {
            throw new BusinessException("Minimum delivery order is " + _options.MinDeliveryAmount.ToString("0") + " ₺.");
        }
    }
}
