using CafeOrder.BusinessLayer.Services.Categories;
using CafeOrder.BusinessLayer.Services.Products;
using CafeOrder.DtoLayer.Dtos.ProductDtos;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.ViewComponents.Products_Detail;

/// <summary>Ürün detayının altındaki "Bunlara da bayılabilirsiniz" bölümü.</summary>
public class _ProductDetailRelatedViewComponent : ViewComponent
{
    private readonly IProductService _productService;

    public _ProductDetailRelatedViewComponent(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>Aynı kategoriden 4 ürün getirir (ürünün kendisi hariç). Liste boşsa view hiçbir şey çizmez.</summary>
    public async Task<IViewComponentResult> InvokeAsync(int productId, int categoryId)
    {
        var related = await _productService.GetRelatedAsync(productId, categoryId, 4);
        return View(related);
    }
}
