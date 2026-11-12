using CafeOrder.DataAccessLayer.Context;
using CafeOrder.DataAccessLayer.Repositories.GenericRepositories;
using CafeOrder.EntityLayer.Entities;

namespace CafeOrder.DataAccessLayer.Repositories.Categories;

public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
{
    public CategoryRepository(CafeOrderDbContext context) : base(context)
    {
    }
}
