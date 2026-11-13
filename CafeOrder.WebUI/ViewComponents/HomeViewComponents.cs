using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.ViewComponents;

// Ana sayfanın statik bölümleri. Veri almazlar; sadece kendi Default.cshtml'lerini çizerler.

/// <summary>Ana sayfa giriş (hero) bölümü. Statik içerik.</summary>
public class HomeHeroViewComponent : ViewComponent
{
    /// <summary>Default.cshtml'i çizer (veri yok).</summary>
    public IViewComponentResult Invoke()
    {
        return View();
    }
}

/// <summary>Ana sayfadaki sipariş yolları (online, QR, gel-al) kartları. Statik içerik.</summary>
public class HomeOrderWaysViewComponent : ViewComponent
{
    /// <summary>Default.cshtml'i çizer (veri yok).</summary>
    public IViewComponentResult Invoke()
    {
        return View();
    }
}

/// <summary>Ana sayfadaki hakkımızda bölümü. Statik içerik.</summary>
public class HomeAboutViewComponent : ViewComponent
{
    /// <summary>Default.cshtml'i çizer (veri yok).</summary>
    public IViewComponentResult Invoke()
    {
        return View();
    }
}

/// <summary>Ana sayfadaki kampanya kutusu. Statik içerik (PINK20 kodu sadece görseldir).</summary>
public class HomeOfferViewComponent : ViewComponent
{
    /// <summary>Default.cshtml'i çizer (veri yok).</summary>
    public IViewComponentResult Invoke()
    {
        return View();
    }
}

/// <summary>Ana sayfadaki misafir yorumları. Statik içerik.</summary>
public class HomeReviewsViewComponent : ViewComponent
{
    /// <summary>Default.cshtml'i çizer (veri yok).</summary>
    public IViewComponentResult Invoke()
    {
        return View();
    }
}
