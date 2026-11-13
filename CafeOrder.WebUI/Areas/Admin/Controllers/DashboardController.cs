using CafeOrder.BusinessLayer.Services.Orders;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.Areas.Admin.Controllers;

/// <summary>Admin ana sayfası (panel). Bütün rakamları OrderService hesaplar; controller sadece DTO'yu sayfaya verir.</summary>
[Route("admin")]
public class DashboardController : AdminControllerBase
{
    private readonly IOrderService _orderService;

    public DashboardController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>Panel sayfası. Model: ResultDashboardDto (bugünkü sipariş/ciro, açık siparişler, son 7 gün, durum sayıları, en çok satanlar, son 6 sipariş).</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        return View(await _orderService.GetDashboardAsync());
    }
}
