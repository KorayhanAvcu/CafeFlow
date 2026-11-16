using CafeOrder.BusinessLayer.Services.Orders;
using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.EntityLayer.Enums;
using CafeOrder.WebUI.Cart;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.ViewComponents.Layout;

/// <summary>Sağdan açılan sepet paneli. Fiyatları OrderPricingService hesaplar. View: Components/_LayoutCartDrawer/Default.cshtml</summary>
public class _LayoutCartDrawerViewComponent : ViewComponent
{
    private readonly IOrderPricingService _orderPricingService;

    public _LayoutCartDrawerViewComponent(IOrderPricingService orderPricingService)
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
