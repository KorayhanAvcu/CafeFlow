using Microsoft.AspNetCore.Mvc;


// Ana sayfanın statik bölümleri. Veri almazlar; sadece kendi Default.cshtml'lerini çizerler.

namespace CafeOrder.WebUI.ViewComponents.Home_Index;

/// <summary>Ana sayfadaki sipariş yolları (online, QR, gel-al) kartları. Statik içerik.</summary>
public class _HomeOrderWaysViewComponent : ViewComponent
{
    /// <summary>Default.cshtml'i çizer (veri yok).</summary>
    public IViewComponentResult Invoke()
    {
        return View();
    }
}
