using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.Controllers;

/// <summary>Ana sayfa ve hata sayfaları.</summary>
public class HomeController : Controller
{
    /// <summary>
    /// Ana sayfa. Controller veri getirmez: sayfa ViewComponent'lerden oluşur ve veri gereken bölümler
    /// (_HomeCategoryShowcase, _HomeFeaturedProducts) veriyi kendisi getirir.
    /// </summary>
    [HttpGet("/")]
    public IActionResult Index()
    {
        return View();
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
