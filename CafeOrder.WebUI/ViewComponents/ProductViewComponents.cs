using CafeOrder.DtoLayer.Dtos.ProductDtos;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.ViewComponents;

// Menü ve ürün detayı parçaları.

/// <summary>Tek ürün kartı: ana sayfa, menü ve benzer ürünlerde kullanılır.</summary>
public class ProductCardViewComponent : ViewComponent
{
    /// <summary>Gelen ResultProductDto nesnesini model olarak Default.cshtml'e verir.</summary>
    public IViewComponentResult Invoke(ResultProductDto product)
    {
        return View(product);
    }
}

/// <summary>Menü sayfasının başlık alanı. Statik içerik.</summary>
public class MenuHeaderViewComponent : ViewComponent
{
    /// <summary>Default.cshtml'i çizer (veri yok).</summary>
    public IViewComponentResult Invoke()
    {
        return View();
    }
}

/// <summary>Menü sayfasının altındaki tanıtım kutusu. Statik içerik.</summary>
public class MenuPromoViewComponent : ViewComponent
{
    /// <summary>Default.cshtml'i çizer (veri yok).</summary>
    public IViewComponentResult Invoke()
    {
        return View();
    }
}

/// <summary>Ürün detayının sayfa yolu ve "Menüye Dön" linki.</summary>
public class DetailBreadcrumbViewComponent : ViewComponent
{
    /// <summary>Gelen ResultProductDto nesnesini model olarak Default.cshtml'e verir.</summary>
    public IViewComponentResult Invoke(ResultProductDto product)
    {
        return View(product);
    }
}

/// <summary>Ürün detayındaki fotoğraf ve rozet.</summary>
public class ProductGalleryViewComponent : ViewComponent
{
    /// <summary>Gelen ResultProductDto nesnesini model olarak Default.cshtml'e verir.</summary>
    public IViewComponentResult Invoke(ResultProductDto product)
    {
        return View(product);
    }
}
