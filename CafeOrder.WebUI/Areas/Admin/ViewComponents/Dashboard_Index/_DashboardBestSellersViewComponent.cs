using CafeOrder.DtoLayer.Dtos.OrderDtos;
using Microsoft.AspNetCore.Mvc;


// Admin sayfalarının parçaları: hata kutusu, panel kartları ve sipariş detayı bölümleri.

namespace CafeOrder.WebUI.Areas.Admin.ViewComponents.Dashboard_Index;

/// <summary>Paneldeki en çok satanlar tablosu.</summary>
public class _DashboardBestSellersViewComponent : ViewComponent
{
    /// <summary>Gelen ResultDashboardDto nesnesini model olarak Default.cshtml'e verir.</summary>
    public IViewComponentResult Invoke(ResultDashboardDto dashboard)
    {
        return View(dashboard);
    }
}
