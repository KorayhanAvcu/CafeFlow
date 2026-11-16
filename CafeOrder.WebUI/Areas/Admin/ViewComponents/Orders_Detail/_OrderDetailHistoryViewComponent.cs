using CafeOrder.DtoLayer.Dtos.OrderDtos;
using Microsoft.AspNetCore.Mvc;


// Admin sayfalarının parçaları: hata kutusu, panel kartları ve sipariş detayı bölümleri.

namespace CafeOrder.WebUI.Areas.Admin.ViewComponents.Orders_Detail;

/// <summary>Sipariş detayındaki durum geçmişi.</summary>
public class _OrderDetailHistoryViewComponent : ViewComponent
{
    /// <summary>Gelen ResultOrderDto nesnesini model olarak Default.cshtml'e verir.</summary>
    public IViewComponentResult Invoke(ResultOrderDto order)
    {
        return View(order);
    }
}
