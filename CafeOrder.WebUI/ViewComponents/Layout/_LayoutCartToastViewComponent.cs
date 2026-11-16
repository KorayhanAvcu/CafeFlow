using Microsoft.AspNetCore.Mvc;


// Müşteri layout'unun ortak parçaları: üst menü, alt bilgi, sepet bildirimi.

namespace CafeOrder.WebUI.ViewComponents.Layout;

/// <summary>Sepet işleminden sonra bir kez çıkan bildirim. Mesajı TempData["CartToast"]'tan okur (PRG).</summary>
public class _LayoutCartToastViewComponent : ViewComponent
{
    /// <summary>TempData'daki tek seferlik mesajı okur (okununca silinir) ve bildirimi çizer; mesaj yoksa bildirim gizli kalır.</summary>
    public IViewComponentResult Invoke()
    {
        var message = TempData["CartToast"] as string;
        // Model string olduğu için view adı açıkça verilir; yoksa View(string) metni view adı sanardı.
        return View("Default", message);
    }
}
