using CafeOrder.DataAccessLayer.Context;
using CafeOrder.DataAccessLayer.Repositories.GenericRepositories;
using CafeOrder.EntityLayer.Entities;

namespace CafeOrder.DataAccessLayer.Repositories.ProductOptions;

public class ProductOptionRepository : GenericRepository<ProductOption>, IProductOptionRepository
{
    public ProductOptionRepository(CafeOrderDbContext context) : base(context)
    {
    }
}
