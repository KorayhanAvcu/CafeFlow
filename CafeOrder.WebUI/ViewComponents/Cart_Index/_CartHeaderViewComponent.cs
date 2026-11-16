using CafeOrder.DtoLayer.Dtos.OrderDtos;
using Microsoft.AspNetCore.Mvc;


// Sepet ve onay sayfası parçaları. Hepsi CartService / OrderService'ten gelen DTO'yu parametre olarak alır.

namespace CafeOrder.WebUI.ViewComponents.Cart_Index;

/// <summary>Sepet sayfasının başlığı. Statik içerik.</summary>
public class _CartHeaderViewComponent : ViewComponent
{
    /// <summary>Default.cshtml'i çizer (veri yok).</summary>
    public IViewComponentResult Invoke()
    {
        return View();
    }
}
