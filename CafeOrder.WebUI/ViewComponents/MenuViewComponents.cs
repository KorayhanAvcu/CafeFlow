using CafeOrder.BusinessLayer.Services.Categories;
using CafeOrder.BusinessLayer.Services.Products;
using CafeOrder.DtoLayer.Dtos.ProductDtos;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.ViewComponents;

/// <summary>Ana sayfadaki "Menüyü keşfedin" kategori kartları. Veriyi kendisi çeker; controller'ın ViewBag doldurmasına gerek kalmaz.</summary>
public class CategoryShowcaseViewComponent : ViewComponent
{
    private readonly ICategoryService _categoryService;

    public CategoryShowcaseViewComponent(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>İçinde ürün olan aktif kategorileri getirir ve kart listesi olarak çizer.</summary>
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var categories = await _categoryService.GetActiveWithProductsAsync();
        return View(categories);
    }
}

/// <summary>Menü sayfasındaki kategori düğmeleri (Tüm Ürünler + her kategori).</summary>
public class CategoryFilterViewComponent : ViewComponent
{
    private readonly ICategoryService _categoryService;

    public CategoryFilterViewComponent(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>Aktif kategorileri getirir; <paramref name="selected"/> slug'ı olan düğmeyi seçili çizer (null ise "Tüm Ürünler").</summary>
    public async Task<IViewComponentResult> InvokeAsync(string? selected)
    {
        var categories = await _categoryService.GetActiveAsync();
        // <vc:...> etiketi boş değeri "" olarak verebilir; "" de "kategori seçilmedi" demektir.
        ViewBag.Selected = string.IsNullOrEmpty(selected) ? null : selected;
        return View(categories);
    }
}

/// <summary>Ürün detayındaki seçenek grupları (boy, süt, ekstra shot, şurup) ve başlangıç fiyatı kutusu.</summary>
public class ProductOptionsViewComponent : ViewComponent
{
    private readonly IProductService _productService;

    public ProductOptionsViewComponent(IProductService productService)
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

/// <summary>Ürün detayının altındaki "Bunlara da bayılabilirsiniz" bölümü.</summary>
public class RelatedProductsViewComponent : ViewComponent
{
    private readonly IProductService _productService;

    public RelatedProductsViewComponent(IProductService productService)
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
