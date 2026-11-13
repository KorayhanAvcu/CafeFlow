using CafeOrder.DtoLayer.Dtos.ProductDtos;
using CafeOrder.DtoLayer.Dtos.ProductOptionDtos;

namespace CafeOrder.BusinessLayer.Services.Products;

public interface IProductService
{
    // Müşteri sayfaları
    /// <summary>Müşteri menüsü: aktif ürünler; kategori slug'ı ve arama metniyle filtrelenebilir.</summary>
    Task<List<ResultProductDto>> GetMenuAsync(string? category, string? search);
    /// <summary>Ana sayfadaki öne çıkanlar: rozeti olan ilk count ürün.</summary>
    Task<List<ResultProductDto>> GetFeaturedAsync(int count);
    /// <summary>Menüde görünen tek ürün (ürün detay sayfası). Pasifse veya yoksa null.</summary>
    Task<ResultProductDto?> GetActiveByIdAsync(int id);
    /// <summary>Aynı kategoriden diğer ürünler ("Yanında iyi gider").</summary>
    Task<List<ResultProductDto>> GetRelatedAsync(int productId, int categoryId, int count);
    /// <summary>Bütün seçenekler: boyut, süt, ekstra shot, şurup.</summary>
    Task<List<ResultProductOptionDto>> GetOptionsAsync();

    // Admin
    /// <summary>Admin listesi: bütün ürünler (pasifler dahil).</summary>
    Task<List<ResultProductDto>> GetAllAsync();
    /// <summary>Admin listesi, kategori Id'si ve ada göre aramayla filtrelenmiş.</summary>
    Task<List<ResultProductDto>> GetAdminListAsync(int? categoryId, string? search);
    /// <summary>Admin formu: id null ise boş "yeni ürün" formu, değilse o ürünün dolu formu.</summary>
    Task<UpdateProductDto> GetFormAsync(int? id);
    /// <summary>Admin formunun kaydet düğmesi: Id 0 ise yeni ürün ekler, değilse günceller.</summary>
    Task SaveAsync(UpdateProductDto dto);
    /// <summary>Yeni ürün ekler (kurallar kontrol edilir).</summary>
    Task CreateAsync(CreateProductDto dto);
    /// <summary>Var olan ürünü formdaki değerlerle günceller.</summary>
    Task UpdateAsync(UpdateProductDto dto);
    /// <summary>Ürünü aktif veya pasif yapar.</summary>
    Task ChangeStatusAsync(int id, bool isActive);
    /// <summary>Ürünü siler; geçmiş siparişlerde varsa BusinessException.</summary>
    Task DeleteAsync(int id);
}
