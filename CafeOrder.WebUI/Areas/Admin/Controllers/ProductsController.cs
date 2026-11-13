using CafeOrder.BusinessLayer.Exceptions;
using CafeOrder.BusinessLayer.Services.Products;
using CafeOrder.DtoLayer.Dtos.ProductDtos;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.Areas.Admin.Controllers;

/// <summary>
/// Ürün yönetimi: listele, ekle, düzenle, aktif/pasif, sil.
/// Controller sadece DTO alıp verir; filtreleme, form hazırlama ve "ekle mi güncelle mi" kararı ProductService'te.
/// </summary>
[Route("admin/products")]
public class ProductsController : AdminControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>Ürün listesi; kategori ve ada göre filtre (filtreyi servis uygular).</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(int? category, string? q)
    {
        ViewBag.Category = category;
        ViewBag.Search = q;
        return View(await _productService.GetAdminListAsync(category, q));
    }

    /// <summary>Boş "yeni ürün" formu. Kategori listesini formdaki CategorySelect ViewComponent'i getirir.</summary>
    [HttpGet("new")]
    public async Task<IActionResult> Create()
    {
        return View("Form", await _productService.GetFormAsync(null));
    }

    /// <summary>Ürünün dolu düzenleme formu; ürün yoksa 404.</summary>
    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            return View("Form", await _productService.GetFormAsync(id));
        }
        catch (BusinessException)
        {
            return NotFound();
        }
    }

    /// <summary>Formun kaydet düğmesi. Form UpdateProductDto olarak gelir; servis Id'ye bakıp ekler ya da günceller.</summary>
    [HttpPost("save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(UpdateProductDto form)
    {
        try
        {
            await _productService.SaveAsync(form);
            return RedirectToAction("Index");
        }
        catch (BusinessException ex)
        {
            // Hata: formu yazılanlarla tekrar göster (kategori listesini CategorySelect ViewComponent'i kendisi yükler).
            ViewBag.Error = ex.Message;
            return View("Form", form);
        }
    }

    /// <summary>Listedeki aktif/pasif düğmesi.</summary>
    [HttpPost("{id:int}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, bool active)
    {
        try
        {
            await _productService.ChangeStatusAsync(id, active);
            return RedirectToAction("Index");
        }
        catch (BusinessException ex)
        {
            return await ShowListWithErrorAsync(ex.Message);
        }
    }

    /// <summary>Listedeki sil düğmesi: ürünü siler; siparişlerde geçiyorsa uyarı mesajıyla listeye döner.</summary>
    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _productService.DeleteAsync(id);
            return RedirectToAction("Index");
        }
        catch (BusinessException ex)
        {
            return await ShowListWithErrorAsync(ex.Message);
        }
    }

    /// <summary>Kural hatasında (ör. siparişte geçen ürünü silme) listeyi hata mesajıyla doğrudan gösterir; yönlendirme yapılmaz.</summary>
    private async Task<IActionResult> ShowListWithErrorAsync(string error)
    {
        ViewBag.Error = error;
        return View("Index", await _productService.GetAdminListAsync(null, null));
    }
}
