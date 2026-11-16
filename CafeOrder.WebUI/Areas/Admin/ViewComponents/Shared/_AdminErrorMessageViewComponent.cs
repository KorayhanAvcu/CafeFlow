using CafeOrder.DtoLayer.Dtos.OrderDtos;
using Microsoft.AspNetCore.Mvc;


// Admin sayfalarının parçaları: hata kutusu, panel kartları ve sipariş detayı bölümleri.

namespace CafeOrder.WebUI.Areas.Admin.ViewComponents.Shared;

/// <summary>Kural hatası mesajını sarı kutuda gösterir; mesaj boşsa hiçbir şey çizmez.</summary>
public class _AdminErrorMessageViewComponent : ViewComponent
{
    /// <summary>Gelen mesajı model olarak Default.cshtml'e verir.</summary>
    public IViewComponentResult Invoke(string? message)
    {
        // Model string olduğu için view adı açıkça verilir; yoksa View(string) metni view adı sanardı.
        return View("Default", message);
    }
}
