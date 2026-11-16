using CafeOrder.BusinessLayer.Services.Products;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.ViewComponents.Home_Index;

/// <summary>
/// Ana sayfadaki "Bu haftanın popülerleri" bölümü: rozetli 6 ürün.
/// Veriyi kendisi getirir (servis üzerinden); HomeController'ın sayfaya model vermesine gerek kalmaz.
/// View: Views/Shared/Components/_HomeFeaturedProducts/Default.cshtml
/// </summary>
public class _HomeFeaturedProductsViewComponent : ViewComponent
{
    private const int FeaturedCount = 6;

    private readonly IProductService _productService;

    public _HomeFeaturedProductsViewComponent(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>Menüde görünen, rozetli ürünlerden ilk 6'sını getirir ve kart listesi olarak çizer.</summary>
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var products = await _productService.GetFeaturedAsync(FeaturedCount);
        return View(products);
    }
}
