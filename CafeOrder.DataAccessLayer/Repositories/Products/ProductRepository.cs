using CafeOrder.DataAccessLayer.Context;
using CafeOrder.DataAccessLayer.Repositories.GenericRepositories;
using CafeOrder.EntityLayer.Entities;

namespace CafeOrder.DataAccessLayer.Repositories.Products;

public class ProductRepository : GenericRepository<Product>, IProductRepository
{
    public ProductRepository(CafeOrderDbContext context) : base(context)
    {
    }
}
