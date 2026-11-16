using CafeOrder.DataAccessLayer.Repositories.GenericRepositories;
using CafeOrder.EntityLayer.Entities;

namespace CafeOrder.DataAccessLayer.Repositories.Categories;

public interface ICategoryRepository : IRepository<Category>
{
    /// <summary>Bütün kategoriler ürünleriyle, sıraya göre (admin listesi).</summary>
    Task<List<Category>> GetAllWithProductsAsync();
    /// <summary>Aktif kategoriler ve içlerindeki aktif ürünler, sıraya göre (müşteri menüsü).</summary>
    Task<List<Category>> GetActiveWithActiveProductsAsync();
}
