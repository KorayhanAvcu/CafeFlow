using Microsoft.AspNetCore.Mvc;


// Ana sayfanın statik bölümleri. Veri almazlar; sadece kendi Default.cshtml'lerini çizerler.

namespace CafeOrder.WebUI.ViewComponents.Home_Index;

/// <summary>Ana sayfadaki hakkımızda bölümü. Statik içerik.</summary>
public class _HomeAboutViewComponent : ViewComponent
{
    /// <summary>Default.cshtml'i çizer (veri yok).</summary>
    public IViewComponentResult Invoke()
    {
        return View();
    }
}
