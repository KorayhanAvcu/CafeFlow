using CafeOrder.BusinessLayer.Exceptions;
using CafeOrder.BusinessLayer.Services.Cart;
using CafeOrder.BusinessLayer.Services.Orders;
using CafeOrder.BusinessLayer.Services.Tables;
using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.DtoLayer.Dtos.TableDtos;
using CafeOrder.EntityLayer.Enums;
using CafeOrder.WebUI.Cart;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.Controllers;

/// <summary>
/// Sepet ve sipariş verme. Controller sadece aracıdır: Session'daki sepeti okur, DTO'larla birlikte
/// CartService'e verir, dönen DTO'yu sayfaya verir. Bütün kurallar ve hesaplar serviste.
/// </summary>
[Route("cart")]
public class CartController : Controller
{
    private readonly ICartService _cartService;
    private readonly IOrderQueryService _orderQueryService;
    private readonly ITableService _tableService;

    public CartController(ICartService cartService, IOrderQueryService orderQueryService, ITableService tableService)
    {
        _cartService = cartService;
        _orderQueryService = orderQueryService;
        _tableService = tableService;
    }

    /// <summary>Sepet sayfası. Masa QR kodu okutulunca da buraya gelinir: /cart?qr=CF-T07-GUL</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(string? qr)
    {
        if (qr != null)
        {
            var table = await _tableService.GetByQrAsync(qr);
            if (table == null)
            {
                TempData["CartError"] = "Bu masa QR kodu geçerli değil. Lütfen masanızdaki kodu tekrar okutun.";
            }
            else
            {
                HttpContext.Session.SetTable(table.QrToken);
                TempData["CartToast"] = "Hoş geldiniz! Masa " + table.Number + " için sipariş verebilirsiniz.";
            }

            // QR kodu adres çubuğunda kalmasın diye sade adrese yönlendir.
            return Redirect("/cart");
        }

        return View(await GetCartPageAsync(null, TempData["CartError"] as string));
    }

    /// <summary>Ürün kartındaki veya ürün detayındaki "Sepete Ekle" düğmesi. Form AddToCartDto olarak gelir.</summary>
    [HttpPost("add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(AddToCartDto dto)
    {
        List<CartItemDto> cart = HttpContext.Session.GetCart();
        try
        {
            var productName = await _cartService.AddItemAsync(cart, dto);
            HttpContext.Session.SaveCart(cart);
            TempData["CartToast"] = productName + " sepete eklendi";
            TempData["OpenCart"] = true;
        }
        catch (BusinessException)
        {
            return NotFound();
        }
        return RedirectBack(dto.ReturnUrl);
    }

    /// <summary>Satırın adedini değiştirir; adet 0 olursa satır silinir. index = sepetteki sıra.</summary>
    [HttpPost("update")]
    [ValidateAntiForgeryToken]
    public IActionResult Update(int index, int quantity, string? returnUrl)
    {
        List<CartItemDto> cart = HttpContext.Session.GetCart();
        _cartService.UpdateQuantity(cart, index, quantity);
        HttpContext.Session.SaveCart(cart);
        return RedirectBack(returnUrl);
    }

    /// <summary>Sepeti boşaltır.</summary>
    [HttpPost("clear")]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        HttpContext.Session.ClearCart();
        return Redirect("/cart");
    }

    /// <summary>Sipariş türünü (teslimat, gel-al, masa) Session'a yazar.</summary>
    [HttpPost("channel")]
    [ValidateAntiForgeryToken]
    public IActionResult Channel(OrderChannel channel)
    {
        HttpContext.Session.SetChannel(channel);
        return Redirect("/cart#orderType");
    }

    /// <summary>"Siparişi Ver" düğmesi. Form CreateOrderDto olarak gelir; kurallar serviste. Hata olursa sayfa yazılanlarla geri gelir.</summary>
    [HttpPost("checkout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CreateOrderDto form)
    {
        try
        {
            var order = await _cartService.CheckoutAsync(
                HttpContext.Session.GetCart(), HttpContext.Session.GetChannel(), HttpContext.Session.GetQrToken(), form);

            HttpContext.Session.ClearCart();
            return Redirect("/cart/confirmation/" + order.OrderNo);
        }
        catch (BusinessException ex)
        {
            // Kural hatası: aynı sayfayı hata mesajıyla tekrar göster (yazılanlar kaybolmasın).
            return View("Index", await GetCartPageAsync(form, ex.Message));
        }
    }

    /// <summary>Sipariş onay sayfası.</summary>
    [HttpGet("confirmation/{orderNo}")]
    public async Task<IActionResult> Confirmation(string orderNo)
    {
        var order = await _orderQueryService.GetByOrderNoAsync(orderNo.ToUpper());
        if (order == null)
        {
            return NotFound();
        }
        return View(order);
    }

    /// <summary>
    /// Sepet sayfasının verisini CartService'ten alır (ResultCartDto) ve
    /// servisin temizlediği sepeti (silinmiş ürünler çıkmış hali) Session'a geri yazar.
    /// </summary>
    private async Task<ResultCartDto> GetCartPageAsync(CreateOrderDto? form, string? error)
    {
        var page = await _cartService.GetCartAsync(
            HttpContext.Session.GetCart(), HttpContext.Session.GetChannel(), HttpContext.Session.GetQrToken(), form, error);
        HttpContext.Session.SaveCart(page.Items);
        return page;
    }

    /// <summary>Geldiği sayfaya döner. Sadece site içi adreslere izin verilir.</summary>
    private IActionResult RedirectBack(string? returnUrl)
    {
        if (returnUrl != null && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }
        return Redirect("/cart");
    }
}
