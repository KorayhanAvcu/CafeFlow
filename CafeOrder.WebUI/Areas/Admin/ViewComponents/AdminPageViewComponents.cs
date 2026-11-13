using CafeOrder.DtoLayer.Dtos.OrderDtos;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.Areas.Admin.ViewComponents;

// Admin sayfalarının parçaları: hata kutusu, panel kartları ve sipariş detayı bölümleri.

/// <summary>Kural hatası mesajını sarı kutuda gösterir; mesaj boşsa hiçbir şey çizmez.</summary>
public class ErrorMessageViewComponent : ViewComponent
{
    /// <summary>Gelen mesajı model olarak Default.cshtml'e verir.</summary>
    public IViewComponentResult Invoke(string? message)
    {
        // Model string olduğu için view adı açıkça verilir; yoksa View(string) metni view adı sanardı.
        return View("Default", message);
    }
}

/// <summary>Panelin üstündeki 4 sayı kartı.</summary>
public class StatCardsViewComponent : ViewComponent
{
    /// <summary>Gelen ResultDashboardDto nesnesini model olarak Default.cshtml'e verir.</summary>
    public IViewComponentResult Invoke(ResultDashboardDto dashboard)
    {
        return View(dashboard);
    }
}

/// <summary>Paneldeki son siparişler tablosu.</summary>
public class LatestOrdersViewComponent : ViewComponent
{
    /// <summary>Gelen ResultDashboardDto nesnesini model olarak Default.cshtml'e verir.</summary>
    public IViewComponentResult Invoke(ResultDashboardDto dashboard)
    {
        return View(dashboard);
    }
}

/// <summary>Paneldeki en çok satanlar tablosu.</summary>
public class BestSellersViewComponent : ViewComponent
{
    /// <summary>Gelen ResultDashboardDto nesnesini model olarak Default.cshtml'e verir.</summary>
    public IViewComponentResult Invoke(ResultDashboardDto dashboard)
    {
        return View(dashboard);
    }
}

/// <summary>Paneldeki durumlara göre sipariş sayıları.</summary>
public class StatusCountsViewComponent : ViewComponent
{
    /// <summary>Gelen ResultDashboardDto nesnesini model olarak Default.cshtml'e verir.</summary>
    public IViewComponentResult Invoke(ResultDashboardDto dashboard)
    {
        return View(dashboard);
    }
}

/// <summary>Sipariş detayındaki satırlar tablosu ve toplamlar.</summary>
public class OrderItemsViewComponent : ViewComponent
{
    /// <summary>Gelen ResultOrderDto nesnesini model olarak Default.cshtml'e verir.</summary>
    public IViewComponentResult Invoke(ResultOrderDto order)
    {
        return View(order);
    }
}

/// <summary>Sipariş detayındaki durum geçmişi.</summary>
public class OrderHistoryViewComponent : ViewComponent
{
    /// <summary>Gelen ResultOrderDto nesnesini model olarak Default.cshtml'e verir.</summary>
    public IViewComponentResult Invoke(ResultOrderDto order)
    {
        return View(order);
    }
}

/// <summary>STATE: siparişin izin verilen sonraki durumları için butonlar (AllowedNextStatuses).</summary>
public class OrderStatusFormViewComponent : ViewComponent
{
    /// <summary>Gelen ResultOrderDto nesnesini model olarak Default.cshtml'e verir.</summary>
    public IViewComponentResult Invoke(ResultOrderDto order)
    {
        return View(order);
    }
}

/// <summary>Sipariş detayındaki masa veya müşteri bilgileri.</summary>
public class OrderCustomerViewComponent : ViewComponent
{
    /// <summary>Gelen ResultOrderDto nesnesini model olarak Default.cshtml'e verir.</summary>
    public IViewComponentResult Invoke(ResultOrderDto order)
    {
        return View(order);
    }
}
