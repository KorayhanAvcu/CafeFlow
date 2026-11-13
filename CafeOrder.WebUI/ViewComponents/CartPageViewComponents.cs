using CafeOrder.DtoLayer.Dtos.OrderDtos;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.ViewComponents;

// Sepet ve onay sayfası parçaları. Hepsi CartService / OrderService'ten gelen DTO'yu parametre olarak alır.

/// <summary>Sepet sayfasının başlığı. Statik içerik.</summary>
public class CartHeaderViewComponent : ViewComponent
{
    /// <summary>Default.cshtml'i çizer (veri yok).</summary>
    public IViewComponentResult Invoke()
    {
        return View();
    }
}

/// <summary>Sepet satırları: ürün, seçenekler, adet +/- ve kaldır.</summary>
public class CartItemsViewComponent : ViewComponent
{
    /// <summary>Gelen ResultCartDto nesnesini model olarak Default.cshtml'e verir.</summary>
    public IViewComponentResult Invoke(ResultCartDto cart)
    {
        return View(cart);
    }
}

/// <summary>Sipariş türü seçimi (Teslimat / Gel-Al / Masa).</summary>
public class ChannelPickerViewComponent : ViewComponent
{
    /// <summary>Gelen ResultCartDto nesnesini model olarak Default.cshtml'e verir.</summary>
    public IViewComponentResult Invoke(ResultCartDto cart)
    {
        return View(cart);
    }
}

/// <summary>Müşteri bilgileri formu; alanlar sipariş türüne göre değişir.</summary>
public class CheckoutFormViewComponent : ViewComponent
{
    /// <summary>Gelen ResultCartDto nesnesini model olarak Default.cshtml'e verir.</summary>
    public IViewComponentResult Invoke(ResultCartDto cart)
    {
        return View(cart);
    }
}

/// <summary>Sipariş özeti: ara toplam, teslimat ücreti (Strategy) ve "Siparişi Ver" butonu.</summary>
public class CartSummaryViewComponent : ViewComponent
{
    /// <summary>Gelen ResultCartDto nesnesini model olarak Default.cshtml'e verir.</summary>
    public IViewComponentResult Invoke(ResultCartDto cart)
    {
        return View(cart);
    }
}

/// <summary>Onay sayfasındaki sipariş fişi.</summary>
public class OrderReceiptViewComponent : ViewComponent
{
    /// <summary>Gelen ResultOrderDto nesnesini model olarak Default.cshtml'e verir.</summary>
    public IViewComponentResult Invoke(ResultOrderDto order)
    {
        return View(order);
    }
}
