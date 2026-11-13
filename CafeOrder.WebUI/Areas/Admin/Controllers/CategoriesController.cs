using CafeOrder.BusinessLayer.Exceptions;
using CafeOrder.BusinessLayer.Services.Categories;
using CafeOrder.DtoLayer.Dtos.CategoryDtos;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.Areas.Admin.Controllers;

/// <summary>
/// Kategori yönetimi: listele, ekle, düzenle, aktif/pasif, sil.
/// Controller sadece DTO alıp verir; form hazırlama ve "ekle mi güncelle mi" kararı CategoryService'te.
/// </summary>
[Route("admin/categories")]
public class CategoriesController : AdminControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>Kategori listesi sayfası (/admin/categories).</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        return View(await _categoryService.GetAllAsync());
    }

    /// <summary>Boş "yeni kategori" formu.</summary>
    [HttpGet("new")]
    public async Task<IActionResult> Create()
    {
        return View("Form", await _categoryService.GetFormAsync(null));
    }

    /// <summary>Kategorinin dolu düzenleme formu; kategori yoksa 404.</summary>
    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            return View("Form", await _categoryService.GetFormAsync(id));
        }
        catch (BusinessException)
        {
            return NotFound();
        }
    }

    /// <summary>Formun kaydet düğmesi. Form UpdateCategoryDto olarak gelir; servis Id'ye bakıp ekler ya da günceller.</summary>
    [HttpPost("save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(UpdateCategoryDto form)
    {
        try
        {
            await _categoryService.SaveAsync(form);
            return RedirectToAction("Index");
        }
        catch (BusinessException ex)
        {
            // Hata: formu yazılanlarla tekrar göster.
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
            await _categoryService.ChangeStatusAsync(id, active);
            return RedirectToAction("Index");
        }
        catch (BusinessException ex)
        {
            return await ShowListWithErrorAsync(ex.Message);
        }
    }

    /// <summary>Listedeki sil düğmesi: kategoriyi siler; ürünü varsa uyarı mesajıyla listeye döner.</summary>
    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _categoryService.DeleteAsync(id);
            return RedirectToAction("Index");
        }
        catch (BusinessException ex)
        {
            return await ShowListWithErrorAsync(ex.Message);
        }
    }

    /// <summary>Kural hatasında (ör. ürünü olan kategoriyi silme) listeyi hata mesajıyla doğrudan gösterir; yönlendirme yapılmaz.</summary>
    private async Task<IActionResult> ShowListWithErrorAsync(string error)
    {
        ViewBag.Error = error;
        return View("Index", await _categoryService.GetAllAsync());
    }
}
