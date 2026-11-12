using AutoMapper;
using CafeOrder.BusinessLayer.Exceptions;
using CafeOrder.BusinessLayer.Services.Categories;
using CafeOrder.BusinessLayer.Services.Products;
using CafeOrder.DtoLayer.Dtos.ProductDtos;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrder.WebUI.Areas.Admin.Controllers;

/// <summary>Ürün yönetimi: listele, ekle, düzenle, aktif/pasif, sil.</summary>
[Route("admin/products")]
public class ProductsController : AdminControllerBase
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly IMapper _mapper;

    public ProductsController(IProductService productService, ICategoryService categoryService, IMapper mapper)
    {
        _productService = productService;
        _categoryService = categoryService;
        _mapper = mapper;
    }

    /// <summary>Ürün listesi; kategori ve ada göre filtre.</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(int? category, string? q)
    {
        List<ResultProductDto> products = await _productService.GetAllAsync();

        if (category != null)
        {
            products = products.Where(p => p.CategoryId == category).ToList();
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            products = products.Where(p => p.Name.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        ViewBag.Categories = await _categoryService.GetAllAsync();
        ViewBag.Category = category;
        ViewBag.Search = q;
        return View(products);
    }

    /// <summary>Boş form.</summary>
    [HttpGet("new")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await _categoryService.GetAllAsync();

        UpdateProductDto form = new UpdateProductDto();   // Id = 0 → yeni kayıt
        form.IsActive = true;
        return View("Form", form);
    }

    /// <summary>Dolu form.</summary>
    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        ViewBag.Categories = await _categoryService.GetAllAsync();
        ResultProductDto product = await _productService.GetByIdAsync(id);
        UpdateProductDto form = _mapper.Map<UpdateProductDto>(product);
        return View("Form", form);
    }

    /// <summary>Formun kaydet düğmesi. Id 0 ise yeni kayıt, değilse güncelleme.</summary>
    [HttpPost("save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(UpdateProductDto form)
    {
        try
        {
            if (form.Id == 0)
            {
                CreateProductDto createDto = _mapper.Map<CreateProductDto>(form);
                await _productService.CreateAsync(createDto);
            }
            else
            {
                await _productService.UpdateAsync(form);
            }

            Flash("info", form.Name + " was saved.");
            return RedirectToAction("Index");
        }
        catch (BusinessException ex)
        {
            // Hata: formu yazılanlarla tekrar göster (kategori listesi tekrar yüklenmeli).
            ViewBag.Error = ex.Message;
            ViewBag.Categories = await _categoryService.GetAllAsync();
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
            Flash("info", active ? "Product is now active." : "Product is now inactive.");
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
            await _productService.DeleteAsync(id);
            Flash("info", "Product was deleted.");
        }
        catch (BusinessException ex)
        {
            Flash("warn", ex.Message);
        }
        return RedirectToAction("Index");
    }
}
