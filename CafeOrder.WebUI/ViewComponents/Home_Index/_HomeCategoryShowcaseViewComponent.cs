using CafeOrder.BusinessLayer.Services.Categories;
using CafeOrder.BusinessLayer.Services.Products;
using CafeOrder.DtoLayer.Dtos.ProductDtos;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.ViewComponents.Home_Index;

/// <summary>Ana sayfadaki "Menüyü keşfedin" kategori kartları. Veriyi kendisi çeker; controller'ın ViewBag doldurmasına gerek kalmaz.</summary>
public class _HomeCategoryShowcaseViewComponent : ViewComponent
{
    private readonly ICategoryService _categoryService;

    public _HomeCategoryShowcaseViewComponent(ICategoryService categoryService)
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
