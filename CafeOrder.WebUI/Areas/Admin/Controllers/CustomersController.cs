using CafeOrder.BusinessLayer.Services.Customers;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.Areas.Admin.Controllers;

/// <summary>Müşteri listesi. Müşteriler sipariş verirken otomatik oluşur; burada sadece listelenir.</summary>
[Route("admin/customers")]
public class CustomersController : AdminControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    /// <summary>Müşteri listesi sayfası; q ile ad veya telefonda arama.</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(string? q)
    {
        ViewBag.Search = q;
        return View(await _customerService.GetAllAsync(q));
    }
}
