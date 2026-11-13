using CafeOrder.BusinessLayer.Services.Products;
using CafeOrder.DtoLayer.Dtos.ProductDtos;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.Controllers;

/// <summary>Müşteri menüsü ve ürün detayı.</summary>
[Route("products")]
public class ProductsController : Controller
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>Menü. Filtreler adres çubuğundan gelir: /products?category=pastalar&amp;q=latte. Kategori düğmelerini CategoryFilter ViewComponent'i çizer; ViewBag'e sadece seçili filtre yazılır.</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(string? category, string? q)
    {
        var products = await _productService.GetMenuAsync(category, q);

        ViewBag.Category = category;
        ViewBag.Search = q;
        return View(products);
    }

    /// <summary>Ürün detayı: /products/7. Seçenekleri ProductOptions, benzer ürünleri RelatedProducts ViewComponent'i getirir.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var product = await _productService.GetActiveByIdAsync(id);
        if (product == null)
        {
            Response.StatusCode = 404;
            return View("NotFound");
        }

        return View(product);
    }
}
