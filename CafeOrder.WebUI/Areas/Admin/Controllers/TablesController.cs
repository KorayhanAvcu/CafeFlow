using CafeOrder.BusinessLayer.Exceptions;
using CafeOrder.BusinessLayer.Services.Tables;
using CafeOrder.DtoLayer.Dtos.TableDtos;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.Areas.Admin.Controllers;

/// <summary>
/// Masa ve QR kodu yönetimi. Liste ve form aynı sayfadadır.
/// Controller sadece DTO alıp verir; form hazırlama ve "ekle mi güncelle mi" kararı TableService'te.
/// </summary>
[Route("admin/tables")]
public class TablesController : AdminControllerBase
{
    private readonly ITableService _tableService;

    public TablesController(ITableService tableService)
    {
        _tableService = tableService;
    }

    /// <summary>Liste + sağdaki form. ?edit=ID verilirse form o masayla dolar; masa yoksa yeni masa formu gösterilir.</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(int? edit)
    {
        try
        {
            ViewBag.Form = await _tableService.GetFormAsync(edit);
        }
        catch (BusinessException)
        {
            ViewBag.Form = await _tableService.GetFormAsync(null);
        }
        return View(await _tableService.GetAllAsync());
    }

    /// <summary>Masa formunun kaydet düğmesi. Form UpdateTableDto olarak gelir; servis Id'ye bakıp ekler ya da günceller. Hata olursa sayfa yazılanlarla tekrar gösterilir.</summary>
    [HttpPost("save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(UpdateTableDto form)
    {
        try
        {
            await _tableService.SaveAsync(form);
            return RedirectToAction("Index");
        }
        catch (BusinessException ex)
        {
            ViewBag.Error = ex.Message;
            ViewBag.Form = form;
            return View("Index", await _tableService.GetAllAsync());
        }
    }

    /// <summary>Yeni QR kodu üretir; eski kart çalışmaz.</summary>
    [HttpPost("{id:int}/regenerate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Regenerate(int id)
    {
        try
        {
            await _tableService.RegenerateQrAsync(id);
            return RedirectToAction("Index");
        }
        catch (BusinessException ex)
        {
            return await ShowListWithErrorAsync(ex.Message);
        }
    }

    /// <summary>Listedeki aktif/pasif düğmesi.</summary>
    [HttpPost("{id:int}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, bool active)
    {
        try
        {
            await _tableService.ChangeStatusAsync(id, active);
            return RedirectToAction("Index");
        }
        catch (BusinessException ex)
        {
            return await ShowListWithErrorAsync(ex.Message);
        }
    }

    /// <summary>Listedeki sil düğmesi: masayı siler; siparişi varsa uyarı mesajıyla listeye döner.</summary>
    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _tableService.DeleteAsync(id);
            return RedirectToAction("Index");
        }
        catch (BusinessException ex)
        {
            return await ShowListWithErrorAsync(ex.Message);
        }
    }

    /// <summary>Kural hatasında (ör. siparişi olan masayı silme) sayfayı hata mesajıyla doğrudan gösterir; yönlendirme yapılmaz.</summary>
    private async Task<IActionResult> ShowListWithErrorAsync(string error)
    {
        ViewBag.Error = error;
        ViewBag.Form = await _tableService.GetFormAsync(null);
        return View("Index", await _tableService.GetAllAsync());
    }
}
