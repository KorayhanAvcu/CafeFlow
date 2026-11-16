using CafeOrder.DtoLayer.Dtos.ProductDtos;
using Microsoft.AspNetCore.Mvc;


// Menü ve ürün detayı parçaları.

namespace CafeOrder.WebUI.ViewComponents.Products_Index;

/// <summary>Menü sayfasının altındaki tanıtım kutusu. Statik içerik.</summary>
public class _MenuPromoViewComponent : ViewComponent
{
    /// <summary>Default.cshtml'i çizer (veri yok).</summary>
    public IViewComponentResult Invoke()
    {
        return View();
    }
}
