using CafeOrder.BusinessLayer.Services.Notifications;
using CafeOrder.BusinessLayer.Services.Orders;
using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.EntityLayer.Enums;
using CafeOrder.WebUI.Areas.Admin.Models;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.Areas.Admin.Controllers;

/// <summary>Admin ana sayfası: bugünün özet rakamları.</summary>
[Route("admin")]
public class DashboardController : AdminControllerBase
{
    private readonly IOrderService _orderService;
    private readonly INotificationService _notificationService;

    public DashboardController(IOrderService orderService, INotificationService notificationService)
    {
        _orderService = orderService;
        _notificationService = notificationService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        List<ResultOrderDto> orders = await _orderService.GetAllAsync();
        DateTime today = DateTime.Today;
        DateTime weekStart = today.AddDays(-6);

        // İptal edilen siparişler ciroya katılmaz.
        List<ResultOrderDto> notCancelled = orders.Where(o => o.Status != OrderStatus.Cancelled).ToList();

        ViewBag.TodayOrders = orders.Count(o => o.CreatedAt >= today);
        ViewBag.TodayRevenue = notCancelled.Where(o => o.CreatedAt >= today).Sum(o => o.Total);
        ViewBag.WeekRevenue = notCancelled.Where(o => o.CreatedAt >= weekStart).Sum(o => o.Total);
        ViewBag.OpenOrders = orders.Count(o => o.Status == OrderStatus.Received || o.Status == OrderStatus.Preparing || o.Status == OrderStatus.Ready);
        ViewBag.UnreadNotifications = await _notificationService.CountUnreadAsync(NotificationTarget.Admin);

        // Her durumda kaç sipariş var?
        Dictionary<OrderStatus, int> statusCounts = new Dictionary<OrderStatus, int>();
        foreach (OrderStatus status in Enum.GetValues<OrderStatus>())
        {
            statusCounts[status] = orders.Count(o => o.Status == status);
        }
        ViewBag.StatusCounts = statusCounts;

        ViewBag.BestSellers = GetBestSellers(notCancelled, 5);

        // Model: son 6 sipariş
        return View(orders.Take(6).ToList());
    }

    /// <summary>En çok satan ürünler: bütün sipariş kalemleri ürün adına göre toplanır.</summary>
    private List<BestSellerRow> GetBestSellers(List<ResultOrderDto> orders, int count)
    {
        Dictionary<string, BestSellerRow> rows = new Dictionary<string, BestSellerRow>();

        foreach (ResultOrderDto order in orders)
        {
            foreach (ResultOrderDetailDto detail in order.Details)
            {
                if (!rows.ContainsKey(detail.ProductName))
                {
                    BestSellerRow newRow = new BestSellerRow();
                    newRow.Name = detail.ProductName;
                    rows[detail.ProductName] = newRow;
                }

                BestSellerRow row = rows[detail.ProductName];
                row.Quantity += detail.Quantity;
                row.Revenue += detail.LineTotal;
            }
        }

        return rows.Values.OrderByDescending(r => r.Quantity).Take(count).ToList();
    }
}
