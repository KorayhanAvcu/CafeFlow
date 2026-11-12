using CafeOrder.BusinessLayer.Exceptions;
using CafeOrder.BusinessLayer.Services.Orders;
using CafeOrder.BusinessLayer.States;
using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.EntityLayer.Enums;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.Areas.Admin.Controllers;

/// <summary>Sipariş yönetimi: liste, detay, durum güncelleme.</summary>
[Route("admin/orders")]
public class OrdersController : AdminControllerBase
{
    private const int PageSize = 10;

    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>Sipariş listesi: durum, kanal ve aramaya göre filtre, sayfa başına 10 sipariş.</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(OrderStatus? status, OrderChannel? channel, string? q, int page = 1)
    {
        List<ResultOrderDto> orders = await _orderService.GetAllAsync();

        // Üstteki kartlar: her durumdaki sipariş sayısı (filtreden önce)
        Dictionary<OrderStatus, int> counts = new Dictionary<OrderStatus, int>();
        foreach (OrderStatus s in Enum.GetValues<OrderStatus>())
        {
            counts[s] = orders.Count(o => o.Status == s);
        }
        ViewBag.Counts = counts;

        // Filtreler
        if (status != null)
        {
            orders = orders.Where(o => o.Status == status).ToList();
        }
        if (channel != null)
        {
            orders = orders.Where(o => o.Channel == channel).ToList();
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            orders = orders.Where(o => MatchesSearch(o, q)).ToList();
        }

        // Sayfalama
        int totalPages = (int)Math.Ceiling(orders.Count / (double)PageSize);
        if (totalPages < 1)
        {
            totalPages = 1;
        }

        ViewBag.Status = status;
        ViewBag.Channel = channel;
        ViewBag.Search = q;
        ViewBag.Total = orders.Count;
        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;

        List<ResultOrderDto> pageItems = orders.Skip((page - 1) * PageSize).Take(PageSize).ToList();
        return View(pageItems);
    }

    /// <summary>Sipariş detayı.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        ResultOrderDto? order = await _orderService.GetByIdAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        // STATE: bu durumdan hangi durumlara geçilebilir? Sayfa sadece bunların düğmesini gösterir.
        OrderState state = OrderState.For(order.Status);
        ViewBag.AllowedNext = state.GetAllowedNext();
        return View(order);
    }

    /// <summary>Detay sayfasındaki durum düğmeleri.</summary>
    [HttpPost("{id:int}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, OrderStatus target, string? note)
    {
        try
        {
            await _orderService.ChangeStatusAsync(id, target, note);
            Flash("info", "Order moved to " + target + ".");
        }
        catch (BusinessException ex)
        {
            Flash("warn", ex.Message);
        }

        return RedirectToAction("Detail", new { id = id });
    }

    /// <summary>Sipariş no, müşteri adı veya telefonda arama (büyük/küçük harf fark etmez).</summary>
    private bool MatchesSearch(ResultOrderDto order, string q)
    {
        if (order.OrderNo.Contains(q, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        if (order.CustomerFullName != null && order.CustomerFullName.Contains(q, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        if (order.CustomerPhone != null && order.CustomerPhone.Contains(q))
        {
            return true;
        }
        return false;
    }
}
