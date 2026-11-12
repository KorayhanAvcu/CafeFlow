using CafeOrder.BusinessLayer.Exceptions;
using CafeOrder.EntityLayer.Entities;

namespace CafeOrder.BusinessLayer.Validation;

/// <summary>Halka 1: sepet boş mu, ürünler hâlâ satışta mı, seçenekler uygun mu?</summary>
public class ProductHandler : OrderValidationHandler
{
    protected override void Check(Order order)
    {
        if (order.Details.Count == 0)
        {
            throw new BusinessException("Your cart is empty.");
        }

        foreach (OrderDetail detail in order.Details)
        {
            // Ürün veya kategorisi pasif yapıldıysa sipariş verilemez.
            if (!detail.Product.IsActive || !detail.Product.Category.IsActive)
            {
                throw new BusinessException(detail.Product.Name + " is no longer available.");
            }

            // Özelleştirilemeyen ürüne seçenek eklenemez.
            if (detail.Options.Count > 0 && !detail.Product.IsCustomizable)
            {
                throw new BusinessException(detail.Product.Name + " cannot be customized.");
            }

            // Aynı gruptan iki seçenek olamaz (örneğin hem Oat hem Almond süt).
            List<string> usedGroups = new List<string>();
            foreach (OrderDetailOption option in detail.Options)
            {
                if (usedGroups.Contains(option.GroupKey))
                {
                    throw new BusinessException("Only one choice is allowed per option group.");
                }
                usedGroups.Add(option.GroupKey);
            }
        }
    }
}
