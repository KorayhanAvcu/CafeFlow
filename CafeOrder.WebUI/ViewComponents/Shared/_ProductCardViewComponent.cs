using CafeOrder.DtoLayer.Dtos.ProductDtos;
using Microsoft.AspNetCore.Mvc;


// Menü ve ürün detayı parçaları.

namespace CafeOrder.WebUI.ViewComponents.Shared;

/// <summary>Tek ürün kartı: ana sayfa, menü ve benzer ürünlerde kullanılır.</summary>
public class _ProductCardViewComponent : ViewComponent
{
    /// <summary>Gelen ResultProductDto nesnesini model olarak Default.cshtml'e verir.</summary>
    public IViewComponentResult Invoke(ResultProductDto product)
    {
        return View(product);
    }
}
