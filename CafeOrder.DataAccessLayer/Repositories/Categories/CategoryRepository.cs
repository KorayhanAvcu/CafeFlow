using CafeOrder.DataAccessLayer.Context;
using CafeOrder.DataAccessLayer.Repositories.GenericRepositories;
using CafeOrder.EntityLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafeOrder.DataAccessLayer.Repositories.Categories;

public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
{
    public CategoryRepository(CafeOrderDbContext context) : base(context)
    {
    }

    /// <summary>Bütün kategoriler ürünleriyle (ürün sayısı için), sıraya göre.</summary>
    public async Task<List<Category>> GetAllWithProductsAsync()
    {
        return await Set
            .Include(c => c.Products)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();
    }

    /// <summary>Sadece aktif kategoriler ve içlerindeki aktif ürünler.</summary>
    public async Task<List<Category>> GetActiveWithActiveProductsAsync()
    {
        return await Set
            .Where(c => c.IsActive)
            .Include(c => c.Products.Where(p => p.IsActive))
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();
    }
}
