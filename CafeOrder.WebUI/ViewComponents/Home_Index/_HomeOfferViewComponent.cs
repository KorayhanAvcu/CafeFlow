using Microsoft.AspNetCore.Mvc;


// Ana sayfanın statik bölümleri. Veri almazlar; sadece kendi Default.cshtml'lerini çizerler.

namespace CafeOrder.WebUI.ViewComponents.Home_Index;

/// <summary>Ana sayfadaki "Tatlı Pembe Menü" öneri kutusu (latte + pasta). Statik içerik; indirim kodu yoktur.</summary>
public class _HomeOfferViewComponent : ViewComponent
{
    /// <summary>Default.cshtml'i çizer (veri yok).</summary>
    public IViewComponentResult Invoke()
    {
        return View();
    }
}
