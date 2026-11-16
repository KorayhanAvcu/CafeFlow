using CafeOrder.BusinessLayer.Services.Categories;
using CafeOrder.BusinessLayer.Services.Notifications;
using CafeOrder.BusinessLayer.Services.Orders;
using CafeOrder.BusinessLayer.Services.Products;
using CafeOrder.EntityLayer.Enums;
using CafeOrder.WebUI.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.Areas.Admin.ViewComponents;

/// <summary>
/// Her admin sayfasındaki sol menü. Menüdeki sayılar her sayfa açılışında veritabanından okunur.
/// View: Areas/Admin/Views/Shared/Components/AdminSidebar/Default.cshtml
/// </summary>
public class AdminSidebarViewComponent : ViewComponent
{
    private readonly ICategoryService _categoryService;
    private readonly IProductService _productService;
    private readonly IOrderQueryService _orderQueryService;
    private readonly INotificationService _notificationService;

    public AdminSidebarViewComponent(
        ICategoryService categoryService,
        IProductService productService,
        IOrderQueryService orderQueryService,
        INotificationService notificationService)
    {
        _categoryService = categoryService;
        _productService = productService;
        _orderQueryService = orderQueryService;
        _notificationService = notificationService;
    }

    /// <param name="activePage">Menüde vurgulanacak sayfa: "orders", "products" ...</param>
    public async Task<IViewComponentResult> InvokeAsync(string activePage)
    {
        ViewBag.ActivePage = activePage;
        // Sayılar SQL COUNT ile alınır; listeler belleğe çekilmez.
        ViewBag.CategoryCount = await _categoryService.CountAsync();
        ViewBag.ProductCount = await _productService.CountAsync();
        ViewBag.NewOrderCount = await _orderQueryService.CountByStatusAsync(OrderStatus.Received);
        ViewBag.UnreadCount = await _notificationService.CountUnreadAsync(NotificationTarget.Admin);

        // Kullanıcının adı giriş çerezinde (claim) durur; veritabanına gitmeye gerek yok.
        string? fullName = UserClaimsPrincipal.FindFirst(AppClaimsPrincipalFactory.FullNameClaim)?.Value;
        ViewBag.UserName = fullName ?? "Yönetici";
        return View();
    }
}
