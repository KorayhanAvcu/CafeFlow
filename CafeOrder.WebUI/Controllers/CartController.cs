using CafeOrder.BusinessLayer.Exceptions;
using CafeOrder.BusinessLayer.Pricing;
using CafeOrder.BusinessLayer.Services.Orders;
using CafeOrder.BusinessLayer.Services.Products;
using CafeOrder.BusinessLayer.Services.Tables;
using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.DtoLayer.Dtos.ProductDtos;
using CafeOrder.DtoLayer.Dtos.TableDtos;
using CafeOrder.EntityLayer.Enums;
using CafeOrder.WebUI.Cart;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.Controllers;

/// <summary>
/// Sepet ve sipariş verme.
/// Sepet Session'da durur (sadece ürün, seçenek ve adet). Fiyatları her seferinde OrderService hesaplar.
/// </summary>
[Route("cart")]
public class CartController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IProductService _productService;
    private readonly ITableService _tableService;
    private readonly PricingOptions _pricing;

    public CartController(IOrderService orderService, IProductService productService, ITableService tableService, PricingOptions pricing)
    {
        _orderService = orderService;
        _productService = productService;
        _tableService = tableService;
        _pricing = pricing;
    }

    /// <summary>Sepet sayfası. Masa QR kodu okutulunca da buraya gelinir: /cart?qr=CF-T07-ROSE</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(string? qr)
    {
        if (qr != null)
        {
            ResultTableDto? table = await _tableService.GetByQrAsync(qr);
            if (table == null)
            {
                TempData["CartError"] = "This table QR code is not valid. Please scan the code on your table again.";
            }
            else
            {
                // Masa kodu ve sipariş türü Session'a yazılır.
                HttpContext.Session.SetString(CartSession.TableKey, table.QrToken);
                HttpContext.Session.SetString(CartSession.ChannelKey, OrderChannel.Table.ToString());
                TempData["CartToast"] = "Welcome to Table " + table.Number + "!";
            }

            // QR kodu adres çubuğunda kalmasın diye sade adrese yönlendir.
            return RedirectToAction("Index");
        }

        ResultOrderDto cart = await BuildCartAsync(new CreateOrderDto());
        return View(cart);
    }

    /// <summary>Ürün kartındaki veya ürün detayındaki "Add" düğmesi.</summary>
    [HttpPost("add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, int[] optionIds, int? sizeId, int? milkId, int? syrupId, int quantity = 1, string? returnUrl = null)
    {
        ResultProductDto? product = await _productService.GetActiveByIdAsync(productId);
        if (product == null)
        {
            return NotFound();
        }

        // Seçilen seçeneklerin Id'lerini tek listede topla.
        // Boy, süt ve şurup radyo düğmesi (tek değer), ekstra shot onay kutusu (liste).
        List<int> options = new List<int>();
        if (product.IsCustomizable)
        {
            options.AddRange(optionIds);
            if (sizeId != null) options.Add(sizeId.Value);
            if (milkId != null) options.Add(milkId.Value);
            if (syrupId != null) options.Add(syrupId.Value);
            options.Sort();   // aynı seçim farklı sırayla gelse de aynı satır sayılsın
        }

        if (quantity < 1) quantity = 1;
        if (quantity > 20) quantity = 20;

        List<CartItemDto> cart = HttpContext.Session.GetCart();

        // Aynı ürün aynı seçeneklerle zaten varsa adedini artır, yoksa yeni satır ekle.
        CartItemDto? sameLine = null;
        foreach (CartItemDto item in cart)
        {
            if (item.ProductId == productId && item.OptionIds.SequenceEqual(options))
            {
                sameLine = item;
                break;
            }
        }

        if (sameLine != null)
        {
            sameLine.Quantity = Math.Min(20, sameLine.Quantity + quantity);
        }
        else
        {
            CartItemDto newLine = new CartItemDto();
            newLine.ProductId = productId;
            newLine.OptionIds = options;
            newLine.Quantity = quantity;
            cart.Add(newLine);
        }

        HttpContext.Session.SaveCart(cart);

        TempData["CartToast"] = product.Name + " added to your cart";
        TempData["OpenCart"] = true;
        return RedirectBack(returnUrl);
    }

    /// <summary>Satırın adedini değiştirir; adet 0 olursa satır silinir. index = sepetteki sıra.</summary>
    [HttpPost("update")]
    [ValidateAntiForgeryToken]
    public IActionResult Update(int index, int quantity, string? returnUrl)
    {
        List<CartItemDto> cart = HttpContext.Session.GetCart();

        if (index >= 0 && index < cart.Count)
        {
            if (quantity <= 0)
            {
                cart.RemoveAt(index);
            }
            else
            {
                cart[index].Quantity = Math.Min(20, quantity);
            }
            HttpContext.Session.SaveCart(cart);
        }

        return RedirectBack(returnUrl);
    }

    /// <summary>Sepeti boşaltır.</summary>
    [HttpPost("clear")]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        HttpContext.Session.SaveCart(new List<CartItemDto>());
        return RedirectToAction("Index");
    }

    /// <summary>Sipariş türünü (teslimat, gel-al, masa) Session'a yazar.</summary>
    [HttpPost("channel")]
    [ValidateAntiForgeryToken]
    public IActionResult Channel(OrderChannel channel)
    {
        HttpContext.Session.SetString(CartSession.ChannelKey, channel.ToString());
        return Redirect("/cart#orderType");
    }

    /// <summary>"Place Order" düğmesi. Kurallar OrderService içinde; hata olursa sayfa yazılanlarla geri gelir.</summary>
    [HttpPost("checkout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CreateOrderDto form)
    {
        // Formdaki ad, telefon, adres, not, saat alanları model binding ile CreateOrderDto'ya dolar.
        // Sipariş türü formdan değil Session'dan gelir.
        OrderChannel? channel = GetChannel();
        if (channel == null)
        {
            form.Channel = OrderChannel.Delivery;
        }
        else
        {
            form.Channel = channel.Value;
        }

        try
        {
            List<CartItemDto> cart = HttpContext.Session.GetCart();
            string? qrToken = HttpContext.Session.GetString(CartSession.TableKey);

            ResultOrderDto order = await _orderService.PlaceOrderAsync(form, cart, qrToken);

            // Sipariş kaydedildi: sepeti boşalt ve onay sayfasına git.
            HttpContext.Session.SaveCart(new List<CartItemDto>());
            return RedirectToAction("Confirmation", new { orderNo = order.OrderNo });
        }
        catch (BusinessException ex)
        {
            // Kural hatası: aynı sayfayı hata mesajıyla tekrar göster (yazılanlar kaybolmasın).
            ViewBag.Error = ex.Message;
            ResultOrderDto cartView = await BuildCartAsync(form);
            return View("Index", cartView);
        }
    }

    /// <summary>Sipariş onay sayfası.</summary>
    [HttpGet("confirmation/{orderNo}")]
    public async Task<IActionResult> Confirmation(string orderNo)
    {
        ResultOrderDto? order = await _orderService.GetByOrderNoAsync(orderNo.ToUpper());
        if (order == null)
        {
            return NotFound();
        }
        return View(order);
    }

    /// <summary>Session'daki sipariş türünü okur; seçilmemişse null.</summary>
    private OrderChannel? GetChannel()
    {
        string? value = HttpContext.Session.GetString(CartSession.ChannelKey);
        OrderChannel channel;
        if (Enum.TryParse(value, out channel))
        {
            return channel;
        }
        return null;
    }

    /// <summary>
    /// Sepet sayfasının modeli: satırlar ve tutarlar OrderService.CalculateAsync ile hesaplanır (ResultOrderDto).
    /// Formda yazılanlar (ad, adres, not, saat) ViewBag.Form ile geri gönderilir ki hata olunca kaybolmasın.
    /// </summary>
    private async Task<ResultOrderDto> BuildCartAsync(CreateOrderDto form)
    {
        List<CartItemDto> cart = HttpContext.Session.GetCart();
        OrderChannel? channel = GetChannel();

        // Tür seçilmediyse gel-al gibi hesapla (ücret gösterilmez).
        OrderChannel calculateAs = OrderChannel.Pickup;
        if (channel != null)
        {
            calculateAs = channel.Value;
        }

        ResultOrderDto order = await _orderService.CalculateAsync(calculateAs, cart);

        // Menüden kalkmış ürünleri sepetten de çıkar.
        List<CartItemDto> kept = new List<CartItemDto>();
        foreach (CartItemDto item in cart)
        {
            if (order.Details.Any(d => d.ProductId == item.ProductId))
            {
                kept.Add(item);
            }
        }
        if (kept.Count != cart.Count)
        {
            HttpContext.Session.SaveCart(kept);
        }

        ViewBag.Form = form;
        ViewBag.Channel = channel;
        ViewBag.Table = await _tableService.GetByQrAsync(HttpContext.Session.GetString(CartSession.TableKey));
        ViewBag.FreeDeliveryFrom = _pricing.FreeDeliveryFrom;
        ViewBag.DeliveryFee = _pricing.DeliveryFee;
        if (ViewBag.Error == null)
        {
            ViewBag.Error = TempData["CartError"];
        }
        return order;
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
