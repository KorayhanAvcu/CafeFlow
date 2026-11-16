using CafeOrder.BusinessLayer.Services.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.Areas.Admin.Controllers;

/// <summary>Admin ana sayfası (panel). Bütün rakamları DashboardService hesaplar; controller sadece DTO'yu sayfaya verir.</summary>
[Route("admin")]
public class DashboardController : AdminControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>Panel sayfası. Model: ResultDashboardDto (bugünkü sipariş/ciro, açık siparişler, son 7 gün, durum sayıları, en çok satanlar, son 6 sipariş).</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        return View(await _dashboardService.GetDashboardAsync());
    }
}
