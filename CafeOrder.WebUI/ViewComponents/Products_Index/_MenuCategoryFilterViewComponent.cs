using CafeOrder.BusinessLayer.Services.Categories;
using CafeOrder.BusinessLayer.Services.Products;
using CafeOrder.DtoLayer.Dtos.ProductDtos;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.ViewComponents.Products_Index;

/// <summary>Menü sayfasındaki kategori düğmeleri (Tüm Ürünler + her kategori).</summary>
public class _MenuCategoryFilterViewComponent : ViewComponent
{
    private readonly ICategoryService _categoryService;

    public _MenuCategoryFilterViewComponent(ICategoryService categoryService)
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
