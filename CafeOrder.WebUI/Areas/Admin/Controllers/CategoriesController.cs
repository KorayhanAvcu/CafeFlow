using AutoMapper;
using CafeOrder.BusinessLayer.Exceptions;
using CafeOrder.BusinessLayer.Services.Categories;
using CafeOrder.DtoLayer.Dtos.CategoryDtos;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.Areas.Admin.Controllers;

/// <summary>Kategori yönetimi: listele, ekle, düzenle, aktif/pasif, sil.</summary>
[Route("admin/categories")]
public class CategoriesController : AdminControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly IMapper _mapper;

    public CategoriesController(ICategoryService categoryService, IMapper mapper)
    {
        _categoryService = categoryService;
        _mapper = mapper;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        return View(await _categoryService.GetAllAsync());
    }

    /// <summary>Boş form.</summary>
    [HttpGet("new")]
    public IActionResult Create()
    {
        UpdateCategoryDto form = new UpdateCategoryDto();   // Id = 0 → yeni kayıt
        form.IsActive = true;
        return View("Form", form);
    }

    /// <summary>Dolu form.</summary>
    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        ResultCategoryDto category = await _categoryService.GetByIdAsync(id);
        UpdateCategoryDto form = _mapper.Map<UpdateCategoryDto>(category);
        return View("Form", form);
    }

    /// <summary>Formun kaydet düğmesi. Id 0 ise yeni kayıt, değilse güncelleme.</summary>
    [HttpPost("save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(UpdateCategoryDto form)
    {
        try
        {
            if (form.Id == 0)
            {
                CreateCategoryDto createDto = _mapper.Map<CreateCategoryDto>(form);
                await _categoryService.CreateAsync(createDto);
            }
            else
            {
                await _categoryService.UpdateAsync(form);
            }

            Flash("info", form.Name + " was saved.");
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
            Flash("info", active ? "Category is now active." : "Category is now inactive.");
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
            await _categoryService.DeleteAsync(id);
            Flash("info", "Category was deleted.");
        }
        catch (BusinessException ex)
        {
            Flash("warn", ex.Message);
        }
        return RedirectToAction("Index");
    }
}
