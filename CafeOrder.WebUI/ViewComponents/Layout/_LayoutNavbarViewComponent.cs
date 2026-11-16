using Microsoft.AspNetCore.Mvc;


// Müşteri layout'unun ortak parçaları: üst menü, alt bilgi, sepet bildirimi.

namespace CafeOrder.WebUI.ViewComponents.Layout;

/// <summary>Müşteri sitesinin üst menüsü. navPage (home | menu | detail | cart | other) aktif linki ve ana sayfadaki şeffaf görünümü belirler.</summary>
public class _LayoutNavbarViewComponent : ViewComponent
{
    /// <summary>Gelen sayfa adını model olarak Default.cshtml'e verir.</summary>
    public IViewComponentResult Invoke(string navPage)
    {
        // Model string olduğu için view adı açıkça verilir; yoksa View(string) metni view adı sanardı.
        return View("Default", navPage);
    }
}
