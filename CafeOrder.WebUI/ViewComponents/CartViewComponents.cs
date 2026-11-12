using CafeOrder.BusinessLayer.Services.Orders;
using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.EntityLayer.Enums;
using CafeOrder.WebUI.Cart;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.ViewComponents;

/// <summary>Navbar'daki sepet sayısı. View: Views/Shared/Components/CartCount/Default.cshtml</summary>
public class CartCountViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        int count = HttpContext.Session.CartCount();
        return View(count);
    }
}

/// <summary>Sağdan açılan sepet paneli. Fiyatları OrderService hesaplar. View: Components/CartDrawer/Default.cshtml</summary>
public class CartDrawerViewComponent : ViewComponent
{
    private readonly IOrderService _orderService;

    public CartDrawerViewComponent(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        // Panelde teslimat ücreti gösterilmez; bu yüzden gel-al gibi hesaplanır.
        ResultOrderDto cart = await _orderService.CalculateAsync(OrderChannel.Pickup, HttpContext.Session.GetCart());
        return View(cart);
    }
}
