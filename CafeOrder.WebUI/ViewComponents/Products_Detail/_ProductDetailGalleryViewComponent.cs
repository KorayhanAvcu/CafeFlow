using CafeOrder.DtoLayer.Dtos.ProductDtos;
using Microsoft.AspNetCore.Mvc;


// Menü ve ürün detayı parçaları.

namespace CafeOrder.WebUI.ViewComponents.Products_Detail;

/// <summary>Ürün detayındaki fotoğraf ve rozet.</summary>
public class _ProductDetailGalleryViewComponent : ViewComponent
{
    /// <summary>Gelen ResultProductDto nesnesini model olarak Default.cshtml'e verir.</summary>
    public IViewComponentResult Invoke(ResultProductDto product)
    {
        return View(product);
    }
}
