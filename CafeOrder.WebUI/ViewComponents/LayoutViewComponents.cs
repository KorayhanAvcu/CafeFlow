using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.ViewComponents;

// Müşteri layout'unun ortak parçaları: üst menü, alt bilgi, sepet bildirimi.

/// <summary>Müşteri sitesinin üst menüsü. navPage (home | menu | detail | cart | other) aktif linki ve ana sayfadaki şeffaf görünümü belirler.</summary>
public class NavbarViewComponent : ViewComponent
{
    /// <summary>Gelen sayfa adını model olarak Default.cshtml'e verir.</summary>
    public IViewComponentResult Invoke(string navPage)
    {
        // Model string olduğu için view adı açıkça verilir; yoksa View(string) metni view adı sanardı.
        return View("Default", navPage);
    }
}

/// <summary>Müşteri sitesinin alt bilgisi. Ana sayfada linkler sayfa içi çapalara (#menu gibi), diğer sayfalarda adreslere gider.</summary>
public class FooterViewComponent : ViewComponent
{
    /// <summary>Gelen sayfa adını model olarak Default.cshtml'e verir.</summary>
    public IViewComponentResult Invoke(string navPage)
    {
        // Model string olduğu için view adı açıkça verilir; yoksa View(string) metni view adı sanardı.
        return View("Default", navPage);
    }
}

/// <summary>Sepet işleminden sonra bir kez çıkan bildirim. Mesajı TempData["CartToast"]'tan okur (PRG).</summary>
public class CartToastViewComponent : ViewComponent
{
    /// <summary>TempData'daki tek seferlik mesajı okur (okununca silinir) ve bildirimi çizer; mesaj yoksa bildirim gizli kalır.</summary>
    public IViewComponentResult Invoke()
    {
        var message = TempData["CartToast"] as string;
        // Model string olduğu için view adı açıkça verilir; yoksa View(string) metni view adı sanardı.
        return View("Default", message);
    }
}
