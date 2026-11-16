using CafeOrder.DtoLayer.Dtos.ProductDtos;
using Microsoft.AspNetCore.Mvc;


// Menü ve ürün detayı parçaları.

namespace CafeOrder.WebUI.ViewComponents.Products_Detail;

/// <summary>Ürün detayının sayfa yolu ve "Menüye Dön" linki.</summary>
public class _ProductDetailBreadcrumbViewComponent : ViewComponent
{
    /// <summary>Gelen ResultProductDto nesnesini model olarak Default.cshtml'e verir.</summary>
    public IViewComponentResult Invoke(ResultProductDto product)
    {
        return View(product);
    }
}
