using CafeOrder.BusinessLayer.Services.Orders;
using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.EntityLayer.Enums;
using CafeOrder.WebUI.Cart;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.ViewComponents;

/// <summary>Navbar'daki sepet sayısı. View: Views/Shared/Components/CartCount/Default.cshtml</summary>
public class CartCountViewComponent : ViewComponent
{
    /// <summary>Navbar'daki sepet rozetini çizer: Session'daki toplam ürün adedi.</summary>
    public IViewComponentResult Invoke()
    {
        int count = HttpContext.Session.CartCount();
        return View(count);
    }
}

/// <summary>Sağdan açılan sepet paneli. Fiyatları OrderPricingService hesaplar. View: Components/CartDrawer/Default.cshtml</summary>
public class CartDrawerViewComponent : ViewComponent
{
    private readonly IOrderPricingService _orderPricingService;

    public CartDrawerViewComponent(IOrderPricingService orderPricingService)
    {
        _orderPricingService = orderPricingService;
    }

    /// <summary>Sağdan açılan sepet panelini çizer; fiyatları OrderPricingService hesaplar (ücretsiz kanal gibi).</summary>
    public async Task<IViewComponentResult> InvokeAsync()
    {
        // Panelde teslimat ücreti gösterilmez; bu yüzden gel-al gibi hesaplanır.
        var cart = await _orderPricingService.CalculateAsync(OrderChannel.Pickup, HttpContext.Session.GetCart());
        return View(cart);
    }
}
