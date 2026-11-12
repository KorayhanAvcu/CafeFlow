using CafeOrder.BusinessLayer.Services.Categories;
using CafeOrder.BusinessLayer.Services.Products;
using CafeOrder.DtoLayer.Dtos.ProductDtos;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.Controllers;

/// <summary>Müşteri menüsü ve ürün detayı.</summary>
[Route("products")]
public class ProductsController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;

    public ProductsController(IProductService productService, ICategoryService categoryService)
    {
        _productService = productService;
        _categoryService = categoryService;
    }

    /// <summary>Menü. Filtreler adres çubuğundan gelir: /products?category=cakes&amp;q=latte</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(string? category, string? q)
    {
        List<ResultProductDto> products = await _productService.GetMenuAsync(category, q);

        ViewBag.Categories = await _categoryService.GetActiveAsync();
        ViewBag.Category = category;
        ViewBag.Search = q;
        return View(products);
    }

    /// <summary>Ürün detayı: /products/7</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        ResultProductDto? product = await _productService.GetActiveByIdAsync(id);
        if (product == null)
        {
            Response.StatusCode = 404;
            return View("NotFound");
        }

        ViewBag.Options = await _productService.GetOptionsAsync();
        ViewBag.Related = await _productService.GetRelatedAsync(product.Id, product.CategoryId, 4);
        return View(product);
    }
}
