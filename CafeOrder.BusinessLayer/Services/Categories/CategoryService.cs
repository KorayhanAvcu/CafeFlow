using System.Text.RegularExpressions;
using MapsterMapper;
using CafeOrder.BusinessLayer.Exceptions;
using CafeOrder.DataAccessLayer.Repositories.Categories;
using CafeOrder.DataAccessLayer.Repositories.Products;
using CafeOrder.DataAccessLayer.UnitOfWork;
using CafeOrder.DtoLayer.Dtos.CategoryDtos;
using CafeOrder.EntityLayer.Entities;

namespace CafeOrder.BusinessLayer.Services.Categories;

/// <summary>
/// Kategori işleri: müşteri menüsü ve admin kategori yönetimi.
/// Dışarıya DTO verir, dışarıdan DTO alır; veritabanıyla repository üzerinden Entity olarak konuşur.
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;
    private readonly IMapper _mapper;

    public CategoryService(
        ICategoryRepository categoryRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        TimeProvider time,
        IMapper mapper)
    {
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _time = time;
        _mapper = mapper;
    }

    /// <summary>Admin listesi: bütün kategoriler, ürünleriyle birlikte (ürün sayısı için).</summary>
    public async Task<List<ResultCategoryDto>> GetAllAsync()
    {
        var categories = await _categoryRepository.GetAllWithProductsAsync();

        return _mapper.Map<List<ResultCategoryDto>>(categories);
    }

    /// <summary>Müşteri menüsü: sadece aktif kategoriler ve içlerindeki aktif ürünler.</summary>
    public async Task<List<ResultCategoryDto>> GetActiveAsync()
    {
        var categories = await _categoryRepository.GetActiveWithActiveProductsAsync();

        return _mapper.Map<List<ResultCategoryDto>>(categories);
    }


    /// <summary>Kategori sayısı (SQL COUNT; admin menüsündeki rozet).</summary>
    public async Task<int> CountAsync()
    {
        return await _categoryRepository.CountAsync();
    }

    /// <summary>Ana sayfa: aktif kategorilerden içinde en az bir aktif ürün olanları döndürür.</summary>
    public async Task<List<ResultCategoryDto>> GetActiveWithProductsAsync()
    {
        var categories = await GetActiveAsync();
        return categories.Where(c => c.ProductCount > 0).ToList();
    }

    /// <summary>
    /// Admin formu için veri. id null ise boş form döner (Id = 0 → "yeni kayıt", aktif işaretli);
    /// değilse o kategorinin değerleriyle dolu form döner.
    /// </summary>
    public async Task<UpdateCategoryDto> GetFormAsync(int? id)
    {
        if (id == null)
        {
            UpdateCategoryDto form = new UpdateCategoryDto();
            form.IsActive = true;
            return form;
        }
        var category = await GetEntityAsync(id.Value);
        return _mapper.Map<UpdateCategoryDto>(category);
    }

    /// <summary>Formun kaydet düğmesi: Id 0 ise yeni kategori olarak ekler, değilse var olanı günceller.</summary>
    public async Task SaveAsync(UpdateCategoryDto dto)
    {
        if (dto.Id == 0)
        {
            await CreateAsync(_mapper.Map<CreateCategoryDto>(dto));
        }
        else
        {
            await UpdateAsync(dto);
        }
    }

    /// <summary>Yeni kategori ekler: kuralları kontrol eder, sıra boşsa en sona koyar, slug/ikon/tarih atar, kaydeder; sonra Id'den CAT-009 gibi kodu üretip ikinci kez kaydeder.</summary>
    public async Task CreateAsync(CreateCategoryDto dto)
    {
        // DTO → Entity
        Category category = _mapper.Map<Category>(dto);
        await CheckAsync(category);

        // Sıra girilmediyse en sona ekle.
        if (category.DisplayOrder == 0)
        {
            var count = await _categoryRepository.CountAsync();
            category.DisplayOrder = count + 1;
        }

        category.Slug = await MakeSlugAsync(category.Name, 0);
        category.Icon = "bi-tag";
        category.CreatedAt = _time.GetLocalNow().Date;
        await _categoryRepository.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        // Kod, veritabanının verdiği Id ile oluşur: CAT-009 gibi. (Id ilk kayıttan sonra dolduğu için iki kayıt gerekir.)
        category.Code = "CAT-" + category.Id.ToString("000");
        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>Formdan gelen değerleri veritabanındaki kayda kopyalar ve kaydeder.</summary>
    public async Task UpdateAsync(UpdateCategoryDto dto)
    {
        var category = await GetEntityAsync(dto.Id);
        int oldDisplayOrder = category.DisplayOrder;

        // DTO'daki alanları var olan kaydın üstüne yaz (Code, Slug, CreatedAt DTO'da yok, değişmez).
        _mapper.Map(dto, category);
        if (dto.DisplayOrder == 0)
        {
            category.DisplayOrder = oldDisplayOrder;   // sıra boş bırakıldıysa eskisi kalsın
        }

        await CheckAsync(category);
        category.Slug = await MakeSlugAsync(category.Name, category.Id);

        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>Kategorinin aktif/pasif durumunu değiştirip kaydeder.</summary>
    public async Task ChangeStatusAsync(int id, bool isActive)
    {
        var category = await GetEntityAsync(id);
        category.IsActive = isActive;
        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>Ürünü olan kategori silinemez.</summary>
    public async Task DeleteAsync(int id)
    {
        var category = await GetEntityAsync(id);

        var hasProducts = await _productRepository.AnyAsync(p => p.CategoryId == id);
        if (hasProducts)
        {
            throw new BusinessException(category.Name + " kategorisinde hâlâ ürün var. Önce ürünleri taşıyın veya silin.");
        }

        _categoryRepository.Remove(category);
        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>Veritabanındaki kaydı getirir; yoksa hata verir.</summary>
    private async Task<Category> GetEntityAsync(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null)
        {
            throw new BusinessException("Kategori bulunamadı.");
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
            throw new BusinessException("Kategori adı 2-100 karakter olmalı.");
        }
        if (category.Description != null && category.Description.Length > 500)
        {
            throw new BusinessException("Açıklama en fazla 500 karakter olabilir.");
        }
        if (category.DisplayOrder < 0 || category.DisplayOrder > 999)
        {
            throw new BusinessException("Sıra 0 ile 999 arasında olmalı (0: en sona ekle).");
        }

        var nameUsed = await _categoryRepository.AnyAsync(c => c.Name == category.Name && c.Id != category.Id);
        if (nameUsed)
        {
            throw new BusinessException("\"" + category.Name + "\" adında bir kategori zaten var.");
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
