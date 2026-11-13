using CafeOrder.BusinessLayer.Services.Categories;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.Areas.Admin.ViewComponents;

/// <summary>Admin ürün filtresi ve ürün formundaki kategori açılır listesi. Kategorileri kendisi çeker.</summary>
public class CategorySelectViewComponent : ViewComponent
{
    private readonly ICategoryService _categoryService;

    public CategorySelectViewComponent(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// Bütün kategorileri getirir ve bir &lt;select&gt; çizer.
    /// name: formdaki alan adı, selected: seçili kategori Id'si, emptyText: doluysa en üste "boş" seçenek eklenir,
    /// elementId: label'ın bağlanacağı id, cssClass: select'in CSS sınıfı.
    /// </summary>
    public async Task<IViewComponentResult> InvokeAsync(string name, int? selected, string? emptyText = null, string? elementId = null, string cssClass = "adm-select")
    {
        var categories = await _categoryService.GetAllAsync();
        ViewBag.Name = name;
        ViewBag.Selected = selected;
        ViewBag.EmptyText = emptyText;
        ViewBag.ElementId = elementId;
        ViewBag.CssClass = cssClass;
        return View(categories);
    }
}
