using CafeOrder.DataAccessLayer.Repositories.GenericRepositories;
using CafeOrder.EntityLayer.Entities;

namespace CafeOrder.DataAccessLayer.Repositories.Products;

public interface IProductRepository : IRepository<Product>
{
    /// <summary>Menüde görünen ürünler (ürün ve kategorisi aktif). categorySlug ve search verilirse filtreler.</summary>
    Task<List<Product>> GetMenuAsync(string? categorySlug, string? search);
    /// <summary>Menüde görünen, rozeti olan ürünler.</summary>
    Task<List<Product>> GetFeaturedAsync(int count);
    /// <summary>Menüde görünen tek ürün; pasifse veya yoksa null.</summary>
    Task<Product?> GetActiveByIdAsync(int id);
    /// <summary>Aynı kategorideki diğer aktif ürünler.</summary>
    Task<List<Product>> GetRelatedAsync(int productId, int categoryId, int count);
    /// <summary>Admin listesi: bütün ürünler kategorileriyle. categoryId ve search verilirse filtreler.</summary>
    Task<List<Product>> GetAdminListAsync(int? categoryId, string? search);
    /// <summary>Id'leri verilen ürünler kategorileriyle (sepet fiyatlandırması).</summary>
    Task<List<Product>> GetByIdsWithCategoryAsync(IEnumerable<int> ids);
}
