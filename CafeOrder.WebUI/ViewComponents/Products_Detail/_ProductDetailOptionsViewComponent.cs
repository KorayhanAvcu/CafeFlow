using CafeOrder.BusinessLayer.Services.Categories;
using CafeOrder.BusinessLayer.Services.Products;
using CafeOrder.DtoLayer.Dtos.ProductDtos;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.ViewComponents.Products_Detail;

/// <summary>Ürün detayındaki seçenek grupları (boy, süt, ekstra shot, şurup) ve başlangıç fiyatı kutusu.</summary>
public class _ProductDetailOptionsViewComponent : ViewComponent
{
    private readonly IProductService _productService;

    public _ProductDetailOptionsViewComponent(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>Bütün seçenekleri getirir; ürünü ViewBag.Product ile view'a verir (özelleştirilebilir mi, taban fiyat).</summary>
    public async Task<IViewComponentResult> InvokeAsync(ResultProductDto product)
    {
        var options = await _productService.GetOptionsAsync();
        ViewBag.Product = product;
        return View(options);
    }
}
