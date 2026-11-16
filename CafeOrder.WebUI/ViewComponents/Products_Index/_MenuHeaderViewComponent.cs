using CafeOrder.DtoLayer.Dtos.ProductDtos;
using Microsoft.AspNetCore.Mvc;


// Menü ve ürün detayı parçaları.

namespace CafeOrder.WebUI.ViewComponents.Products_Index;

/// <summary>Menü sayfasının başlık alanı. Statik içerik.</summary>
public class _MenuHeaderViewComponent : ViewComponent
{
    /// <summary>Default.cshtml'i çizer (veri yok).</summary>
    public IViewComponentResult Invoke()
    {
        return View();
    }
}
