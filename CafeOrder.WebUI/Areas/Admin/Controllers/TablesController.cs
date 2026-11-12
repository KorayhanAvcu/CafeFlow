using AutoMapper;
using CafeOrder.BusinessLayer.Exceptions;
using CafeOrder.BusinessLayer.Services.Tables;
using CafeOrder.DtoLayer.Dtos.TableDtos;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.Areas.Admin.Controllers;

/// <summary>Masa ve QR kodu yönetimi. Liste ve form aynı sayfadadır.</summary>
[Route("admin/tables")]
public class TablesController : AdminControllerBase
{
    private readonly ITableService _tableService;
    private readonly IMapper _mapper;

    public TablesController(ITableService tableService, IMapper mapper)
    {
        _tableService = tableService;
        _mapper = mapper;
    }

    /// <summary>Liste + sağdaki form. ?edit=ID verilirse form o masayla dolar.</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(int? edit)
    {
        List<ResultTableDto> tables = await _tableService.GetAllAsync();
        ViewBag.Form = GetFormTable(tables, edit);
        return View(tables);
    }

    [HttpPost("save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(UpdateTableDto form)
    {
        try
        {
            if (form.Id == 0)
            {
                CreateTableDto createDto = _mapper.Map<CreateTableDto>(form);
                await _tableService.CreateAsync(createDto);
            }
            else
            {
                await _tableService.UpdateAsync(form);
            }

            Flash("info", "Table " + form.Number + " was saved.");
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
            Flash("info", "A new QR code was created. The old card no longer works.");
        }
        catch (BusinessException ex)
        {
            Flash("warn", ex.Message);
        }
        return RedirectToAction("Index");
    }

    [HttpPost("{id:int}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, bool active)
    {
        try
        {
            await _tableService.ChangeStatusAsync(id, active);
            Flash("info", active ? "Table is now active." : "Table is now inactive.");
        }
        catch (BusinessException ex)
        {
            Flash("warn", ex.Message);
        }
        return RedirectToAction("Index");
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _tableService.DeleteAsync(id);
            Flash("info", "Table was deleted.");
        }
        catch (BusinessException ex)
        {
            Flash("warn", ex.Message);
        }
        return RedirectToAction("Index");
    }

    /// <summary>Sağdaki form için masa: düzenlenen masa ya da yeni masa (numara = en büyük + 1).</summary>
    private UpdateTableDto GetFormTable(List<ResultTableDto> tables, int? editId)
    {
        ResultTableDto? editing = tables.FirstOrDefault(t => t.Id == editId);
        if (editing != null)
        {
            return _mapper.Map<UpdateTableDto>(editing);
        }

        UpdateTableDto newTable = new UpdateTableDto();   // Id = 0 → yeni kayıt
        newTable.IsActive = true;
        if (tables.Count == 0)
        {
            newTable.Number = 1;
        }
        else
        {
            newTable.Number = tables.Max(t => t.Number) + 1;
        }
        return newTable;
    }
}
