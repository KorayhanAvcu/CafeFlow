using CafeOrder.BusinessLayer.Exceptions;
using CafeOrder.EntityLayer.Entities;

namespace CafeOrder.BusinessLayer.Validation;

/// <summary>Halka 2: her satırda adet 1 ile 20 arasında olmalı.</summary>
public class QuantityHandler : OrderValidationHandler
{
    protected override void Check(Order order)
    {
        foreach (OrderDetail detail in order.Details)
        {
            if (detail.Quantity < 1 || detail.Quantity > 20)
            {
                throw new BusinessException("Quantity must be between 1 and 20.");
            }
        }
    }
}
