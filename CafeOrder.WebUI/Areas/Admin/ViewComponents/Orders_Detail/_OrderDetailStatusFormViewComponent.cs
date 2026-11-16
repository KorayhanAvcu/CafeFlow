using CafeOrder.DtoLayer.Dtos.OrderDtos;
using Microsoft.AspNetCore.Mvc;


// Admin sayfalarının parçaları: hata kutusu, panel kartları ve sipariş detayı bölümleri.

namespace CafeOrder.WebUI.Areas.Admin.ViewComponents.Orders_Detail;

/// <summary>STATE: siparişin izin verilen sonraki durumları için butonlar (AllowedNextStatuses).</summary>
public class _OrderDetailStatusFormViewComponent : ViewComponent
{
    /// <summary>Gelen ResultOrderDto nesnesini model olarak Default.cshtml'e verir.</summary>
    public IViewComponentResult Invoke(ResultOrderDto order)
    {
        return View(order);
    }
}
