using CafeOrder.DtoLayer.Dtos.CategoryDtos;

namespace CafeOrder.BusinessLayer.Services.Categories;

public interface ICategoryService
{
    /// <summary>Admin listesi: bütün kategoriler (pasifler dahil), ürün sayılarıyla.</summary>
    Task<List<ResultCategoryDto>> GetAllAsync();
    /// <summary>Müşteri menüsü: sadece aktif kategoriler, aktif ürün sayılarıyla.</summary>
    Task<List<ResultCategoryDto>> GetActiveAsync();
    /// <summary>Ana sayfa: aktif ve içinde en az bir aktif ürün olan kategoriler.</summary>
    Task<List<ResultCategoryDto>> GetActiveWithProductsAsync();
    /// <summary>Admin formu: id null ise boş "yeni kategori" formu, değilse o kategorinin dolu formu.</summary>
    Task<UpdateCategoryDto> GetFormAsync(int? id);
    /// <summary>Admin formunun kaydet düğmesi: Id 0 ise yeni kategori ekler, değilse günceller.</summary>
    Task SaveAsync(UpdateCategoryDto dto);
    /// <summary>Yeni kategori ekler; kod, slug ve tarih sunucuda üretilir.</summary>
    Task CreateAsync(CreateCategoryDto dto);
    /// <summary>Var olan kategoriyi formdaki değerlerle günceller.</summary>
    Task UpdateAsync(UpdateCategoryDto dto);
    /// <summary>Kategoriyi aktif veya pasif yapar.</summary>
    Task ChangeStatusAsync(int id, bool isActive);
    /// <summary>Kategoriyi siler; içinde ürün varsa BusinessException.</summary>
    Task DeleteAsync(int id);
}
