using CafeOrder.BusinessLayer.Exceptions;
using CafeOrder.EntityLayer.Entities;

namespace CafeOrder.BusinessLayer.Validation;

/// <summary>Halka 1: sepet boş mu, ürünler hâlâ satışta mı, seçenekler uygun mu?</summary>
public class ProductHandler : OrderValidationHandler
{
    /// <summary>Halka 1: sepet boş mu, ürün ve kategorisi hâlâ aktif mi, özelleştirilemeyen ürüne seçenek eklenmiş mi, aynı gruptan (ör. iki süt) iki seçim var mı kontrol eder.</summary>
    protected override void Check(Order order)
    {
        if (order.Details.Count == 0)
        {
            throw new BusinessException("Sepetiniz boş.");
        }

        foreach (OrderDetail detail in order.Details)
        {
            // Ürün veya kategorisi pasif yapıldıysa sipariş verilemez.
            if (!detail.Product.IsActive || !detail.Product.Category.IsActive)
            {
                throw new BusinessException(detail.Product.Name + " artık satışta değil.");
            }

            // Özelleştirilemeyen ürüne seçenek eklenemez.
            if (detail.Options.Count > 0 && !detail.Product.IsCustomizable)
            {
                throw new BusinessException(detail.Product.Name + " özelleştirilemez.");
            }

            // Aynı gruptan iki seçenek olamaz (örneğin hem Oat hem Almond süt).
            List<string> usedGroups = new List<string>();
            foreach (OrderDetailOption option in detail.Options)
            {
                if (usedGroups.Contains(option.GroupKey))
                {
                    throw new BusinessException("Her seçenek grubundan yalnızca bir seçim yapılabilir.");
                }
                usedGroups.Add(option.GroupKey);
            }
        }
    }
}
