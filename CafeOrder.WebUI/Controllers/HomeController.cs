using System.Diagnostics;
using CafeOrder.BusinessLayer.Services.Categories;
using CafeOrder.BusinessLayer.Services.Products;
using CafeOrder.DtoLayer.Dtos.CategoryDtos;
using CafeOrder.DtoLayer.Dtos.ProductDtos;
using CafeOrder.WebUI.Models;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.Controllers;

/// <summary>Ana sayfa ve hata sayfaları.</summary>
public class HomeController : Controller
{
    private readonly ICategoryService _categoryService;
    private readonly IProductService _productService;

    public HomeController(ICategoryService categoryService, IProductService productService)
    {
        _categoryService = categoryService;
        _productService = productService;
    }

    [HttpGet("/")]
    public async Task<IActionResult> Index()
    {
        // Ürünü olmayan kategoriler ana sayfada gösterilmez.
        List<ResultCategoryDto> categories = await _categoryService.GetActiveAsync();
        List<ResultCategoryDto> categoriesWithProducts = new List<ResultCategoryDto>();
        foreach (ResultCategoryDto category in categories)
        {
            if (category.ProductCount > 0)
            {
                categoriesWithProducts.Add(category);
            }
        }
        ViewBag.Categories = categoriesWithProducts;

        // Öne çıkan (rozetli) 6 ürün sayfanın modelidir.
        List<ResultProductDto> featured = await _productService.GetFeaturedAsync(6);
        return View(featured);
    }

    /// <summary>Beklenmeyen hata sayfası (Production ortamında).</summary>
    [HttpGet("/error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        ErrorViewModel model = new ErrorViewModel();
        model.RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        return View(model);
    }

    /// <summary>404 gibi durum sayfaları. HTTP fiili yazılmadı: hatalı bir POST da buraya düşebilsin.</summary>
    [Route("/status/{code:int}")]
    public IActionResult Status(int code)
    {
        Response.StatusCode = code;
        return View(code);
    }
}
