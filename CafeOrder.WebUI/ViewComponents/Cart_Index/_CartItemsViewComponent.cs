using CafeOrder.DtoLayer.Dtos.OrderDtos;
using Microsoft.AspNetCore.Mvc;


// Sepet ve onay sayfası parçaları. Hepsi CartService / OrderService'ten gelen DTO'yu parametre olarak alır.

namespace CafeOrder.WebUI.ViewComponents.Cart_Index;

/// <summary>Sepet satırları: ürün, seçenekler, adet +/- ve kaldır.</summary>
public class _CartItemsViewComponent : ViewComponent
{
    /// <summary>Gelen ResultCartDto nesnesini model olarak Default.cshtml'e verir.</summary>
    public IViewComponentResult Invoke(ResultCartDto cart)
    {
        return View(cart);
    }
}
