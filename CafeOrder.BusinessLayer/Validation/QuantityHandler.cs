using CafeOrder.BusinessLayer.Exceptions;
using CafeOrder.EntityLayer.Entities;

namespace CafeOrder.BusinessLayer.Validation;

/// <summary>Halka 2: her satırda adet 1 ile 20 arasında olmalı.</summary>
public class QuantityHandler : OrderValidationHandler
{
    /// <summary>Halka 2: her kalemin adedinin 1 ile 20 arasında olduğunu kontrol eder.</summary>
    protected override void Check(Order order)
    {
        foreach (OrderDetail detail in order.Details)
        {
            if (detail.Quantity < 1 || detail.Quantity > 20)
            {
                throw new BusinessException("Adet 1 ile 20 arasında olmalı.");
            }
        }
    }
}
