using CafeOrder.BusinessLayer.Services.Orders;
using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.EntityLayer.Enums;
using CafeOrder.WebUI.Cart;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.ViewComponents.Layout;

/// <summary>Navbar'daki sepet sayısı. View: Views/Shared/Components/_LayoutCartCount/Default.cshtml</summary>
public class _LayoutCartCountViewComponent : ViewComponent
{
    /// <summary>Navbar'daki sepet rozetini çizer: Session'daki toplam ürün adedi.</summary>
    public IViewComponentResult Invoke()
    {
        int count = HttpContext.Session.CartCount();
        return View(count);
    }
}
