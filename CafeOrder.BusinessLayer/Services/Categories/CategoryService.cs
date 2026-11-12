using System.Text.RegularExpressions;
using AutoMapper;
using CafeOrder.BusinessLayer.Exceptions;
using CafeOrder.DataAccessLayer.Repositories.Categories;
using CafeOrder.DataAccessLayer.Repositories.Products;
using CafeOrder.DtoLayer.Dtos.CategoryDtos;
using CafeOrder.EntityLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafeOrder.BusinessLayer.Services.Categories;

/// <summary>
/// Kategori işleri: müşteri menüsü ve admin kategori yönetimi.
/// Dışarıya DTO verir, dışarıdan DTO alır; veritabanıyla repository üzerinden Entity olarak konuşur.
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public CategoryService(ICategoryRepository categoryRepository, IProductRepository productRepository, IMapper mapper)
    {
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
        _mapper = mapper;
    }

    /// <summary>Admin listesi: bütün kategoriler, ürünleriyle birlikte (ürün sayısı için).</summary>
    public async Task<List<ResultCategoryDto>> GetAllAsync()
    {
        List<Category> categories = await _categoryRepository.GetQueryable()
            .Include(c => c.Products)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        return _mapper.Map<List<ResultCategoryDto>>(categories);
    }

    /// <summary>Müşteri menüsü: sadece aktif kategoriler ve içlerindeki aktif ürünler.</summary>
    public async Task<List<ResultCategoryDto>> GetActiveAsync()
    {
        List<Category> categories = await _categoryRepository.GetQueryable()
            .Where(c => c.IsActive)
            .Include(c => c.Products.Where(p => p.IsActive))
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        return _mapper.Map<List<ResultCategoryDto>>(categories);
    }

    public async Task<ResultCategoryDto> GetByIdAsync(int id)
    {
        Category category = await GetEntityAsync(id);
        return _mapper.Map<ResultCategoryDto>(category);
    }

    public async Task CreateAsync(CreateCategoryDto dto)
    {
        // DTO → Entity
        Category category = _mapper.Map<Category>(dto);
        await CheckAsync(category);

        // Sıra girilmediyse en sona ekle.
        if (category.DisplayOrder == 0)
        {
            int count = await _categoryRepository.CountAsync();
            category.DisplayOrder = count + 1;
        }

        category.Slug = await MakeSlugAsync(category.Name, 0);
        category.Icon = "bi-tag";
        category.CreatedAt = DateTime.Today;
        await _categoryRepository.CreateAsync(category);

        // Kod, veritabanının verdiği Id ile oluşur: CAT-009 gibi.
        category.Code = "CAT-" + category.Id.ToString("000");
        await _categoryRepository.UpdateAsync(category);
    }

    /// <summary>Formdan gelen değerleri veritabanındaki kayda kopyalar ve kaydeder.</summary>
    public async Task UpdateAsync(UpdateCategoryDto dto)
    {
        Category category = await GetEntityAsync(dto.Id);
        int oldDisplayOrder = category.DisplayOrder;

        // DTO'daki alanları var olan kaydın üstüne yaz (Code, Slug, CreatedAt DTO'da yok, değişmez).
        _mapper.Map(dto, category);
        if (dto.DisplayOrder == 0)
        {
            category.DisplayOrder = oldDisplayOrder;   // sıra boş bırakıldıysa eskisi kalsın
        }

        await CheckAsync(category);
        category.Slug = await MakeSlugAsync(category.Name, category.Id);

        await _categoryRepository.UpdateAsync(category);
    }

    public async Task ChangeStatusAsync(int id, bool isActive)
    {
        Category category = await GetEntityAsync(id);
        category.IsActive = isActive;
        await _categoryRepository.UpdateAsync(category);
    }

    /// <summary>Ürünü olan kategori silinemez.</summary>
    public async Task DeleteAsync(int id)
    {
        Category category = await GetEntityAsync(id);

        bool hasProducts = await _productRepository.AnyAsync(p => p.CategoryId == id);
        if (hasProducts)
        {
            throw new BusinessException(category.Name + " still has products. Move or delete them first.");
        }

        await _categoryRepository.DeleteAsync(category);
    }

    /// <summary>Veritabanındaki kaydı getirir; yoksa hata verir.</summary>
    private async Task<Category> GetEntityAsync(int id)
    {
        Category? category = await _categoryRepository.GetByIdAsync(id);
        if (category == null)
        {
            throw new BusinessException("Category not found.");
        }
        return category;
    }

    /// <summary>Kaydetmeden önceki kontroller.</summary>
    private async Task CheckAsync(Category category)
    {
        category.Name = (category.Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(category.Description))
        {
            category.Description = null;
        }
        if (string.IsNullOrWhiteSpace(category.ImageUrl))
        {
            category.ImageUrl = null;
        }

        if (category.Name.Length < 2 || category.Name.Length > 100)
        {
            throw new BusinessException("Category name must be 2-100 characters.");
        }
        if (category.Description != null && category.Description.Length > 500)
        {
            throw new BusinessException("Description can be at most 500 characters.");
        }
        if (category.DisplayOrder < 0 || category.DisplayOrder > 999)
        {
            throw new BusinessException("Display order must be between 1 and 999.");
        }

        bool nameUsed = await _categoryRepository.AnyAsync(c => c.Name == category.Name && c.Id != category.Id);
        if (nameUsed)
        {
            throw new BusinessException("\"" + category.Name + "\" already exists.");
        }
    }

    /// <summary>
    /// Adresde kullanılan kısa ad: "Tea &amp; Infusions" → "tea-infusions".
    /// Aynısı varsa sonuna -2, -3 eklenir.
    /// </summary>
    private async Task<string> MakeSlugAsync(string name, int id)
    {
        string text = name.ToLower()
            .Replace("ç", "c").Replace("ğ", "g").Replace("ı", "i")
            .Replace("ö", "o").Replace("ş", "s").Replace("ü", "u");

        // Harf ve rakam dışındaki her şeyi tireye çevir.
        string slug = Regex.Replace(text, "[^a-z0-9]+", "-").Trim('-');
        if (slug == "")
        {
            slug = "category";
        }

        string result = slug;
        int number = 2;
        while (await _categoryRepository.AnyAsync(c => c.Slug == result && c.Id != id))
        {
            result = slug + "-" + number;
            number++;
        }
        return result;
    }
}
