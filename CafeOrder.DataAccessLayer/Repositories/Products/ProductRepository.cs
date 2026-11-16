using CafeOrder.DataAccessLayer.Context;
using CafeOrder.DataAccessLayer.Repositories.GenericRepositories;
using CafeOrder.EntityLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafeOrder.DataAccessLayer.Repositories.Products;

public class ProductRepository : GenericRepository<Product>, IProductRepository
{
    public ProductRepository(CafeOrderDbContext context) : base(context)
    {
    }

    /// <summary>Menü: kategori (slug) ve arama filtresi; kategori sırası, sonra Id.</summary>
    public async Task<List<Product>> GetMenuAsync(string? categorySlug, string? search)
    {
        IQueryable<Product> query = Active();

        if (!string.IsNullOrEmpty(categorySlug))
        {
            query = query.Where(p => p.Category.Slug == categorySlug);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Name.Contains(search) || p.Category.Name.Contains(search));
        }

        return await query
            .OrderBy(p => p.Category.DisplayOrder)
            .ThenBy(p => p.Id)
            .ToListAsync();
    }

    /// <summary>Rozeti olan aktif ürünler.</summary>
    public async Task<List<Product>> GetFeaturedAsync(int count)
    {
        return await Active()
            .Where(p => p.Badge != null)
            .OrderBy(p => p.Category.DisplayOrder)
            .ThenBy(p => p.Id)
            .Take(count)
            .ToListAsync();
    }

    /// <summary>Menüde görünen tek ürün.</summary>
    public async Task<Product?> GetActiveByIdAsync(int id)
    {
        return await Active().FirstOrDefaultAsync(p => p.Id == id);
    }

    /// <summary>Aynı kategoriden diğer aktif ürünler.</summary>
    public async Task<List<Product>> GetRelatedAsync(int productId, int categoryId, int count)
    {
        return await Active()
            .Where(p => p.CategoryId == categoryId && p.Id != productId)
            .OrderBy(p => p.Id)
            .Take(count)
            .ToListAsync();
    }

    /// <summary>Admin listesi; filtreler SQL'de uygulanır (arama büyük/küçük harfe duyarsız: SQL Server collation).</summary>
    public async Task<List<Product>> GetAdminListAsync(int? categoryId, string? search)
    {
        IQueryable<Product> query = Set.Include(p => p.Category);

        if (categoryId != null)
        {
            query = query.Where(p => p.CategoryId == categoryId);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Name.Contains(search));
        }

        return await query
            .OrderBy(p => p.Category.DisplayOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    /// <summary>Id'leri verilen ürünler kategorileriyle.</summary>
    public async Task<List<Product>> GetByIdsWithCategoryAsync(IEnumerable<int> ids)
    {
        List<int> idList = ids.Distinct().ToList();
        return await Set
            .Include(p => p.Category)
            .Where(p => idList.Contains(p.Id))
            .ToListAsync();
    }

    /// <summary>Menüde görünen ürünler: ürün de kategorisi de aktif olmalı.</summary>
    private IQueryable<Product> Active()
    {
        return Set
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.Category.IsActive);
    }
}
