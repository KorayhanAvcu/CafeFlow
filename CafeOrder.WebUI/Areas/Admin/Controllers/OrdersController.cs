using CafeOrder.BusinessLayer.Exceptions;
using CafeOrder.BusinessLayer.Services.Orders;
using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.EntityLayer.Enums;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.Areas.Admin.Controllers;

/// <summary>
/// Sipariş yönetimi: liste, detay, durum güncelleme.
/// Filtreleme, sayfalama ve izinli durumların hesabı OrderService'te; controller sadece DTO alıp verir.
/// </summary>
[Route("admin/orders")]
public class OrdersController : AdminControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Sipariş listesi. Filtreler adres çubuğundan OrderFilterDto olarak gelir (?status=...&amp;channel=...&amp;q=...&amp;page=...).
    /// Model: ResultOrderListDto (bu sayfanın siparişleri, durum sayıları, sayfa bilgisi).
    /// </summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(OrderFilterDto filter)
    {
        return View(await _orderService.GetAdminListAsync(filter));
    }

    /// <summary>Sipariş detayı. ResultOrderDto.AllowedNextStatuses sayesinde sayfa sadece izinli durum düğmelerini gösterir.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var order = await _orderService.GetByIdAsync(id);
        if (order == null)
        {
            return NotFound();
        }
        return View(order);
    }

    /// <summary>Detay sayfasındaki durum düğmeleri. İzin kontrolü (State) serviste yapılır.</summary>
    [HttpPost("{id:int}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, OrderStatus target, string? note)
    {
        try
        {
            await _orderService.ChangeStatusAsync(id, target, note);
            return Redirect("/admin/orders/" + id);
        }
        catch (BusinessException ex)
        {
            // Kural hatası (ör. izinsiz geçiş): detay sayfasını hata mesajıyla doğrudan göster.
            var order = await _orderService.GetByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            ViewBag.Error = ex.Message;
            return View("Detail", order);
        }
    }
}
