using System.Diagnostics;
using CafeOrder.BusinessLayer.Services.Products;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.Controllers;

/// <summary>Ana sayfa ve hata sayfaları.</summary>
public class HomeController : Controller
{
    private readonly IProductService _productService;

    public HomeController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>Ana sayfa: rozetli 6 öne çıkan ürün (model). Kategori kartlarını CategoryShowcase ViewComponent'i kendisi getirir; bölümler partial'lara ayrılmıştır.</summary>
    [HttpGet("/")]
    public async Task<IActionResult> Index()
    {
        return View(await _productService.GetFeaturedAsync(6));
    }

    /// <summary>Beklenmeyen hata sayfası (Production ortamında). Model: hatayı loglarda bulmaya yarayan istek numarası.</summary>
    [HttpGet("/error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        string requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        return View("Error", requestId);
    }

    /// <summary>404 gibi durum sayfaları. HTTP fiili yazılmadı: hatalı bir POST da buraya düşebilsin.</summary>
    [Route("/status/{code:int}")]
    public IActionResult Status(int code)
    {
        Response.StatusCode = code;
        return View(code);
    }
}
