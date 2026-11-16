using CafeOrder.DataAccessLayer.Context;
using CafeOrder.DataAccessLayer.Repositories.GenericRepositories;
using CafeOrder.EntityLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafeOrder.DataAccessLayer.Repositories.Customers;

public class CustomerRepository : GenericRepository<Customer>, ICustomerRepository
{
    public CustomerRepository(CafeOrderDbContext context) : base(context)
    {
    }

    /// <summary>Müşterileri siparişleriyle getirir; arama ad ve telefonda yapılır.</summary>
    public async Task<List<Customer>> SearchWithOrdersAsync(string? search)
    {
        IQueryable<Customer> query = Set.Include(c => c.Orders);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.FullName.Contains(search) || c.Phone.Contains(search));
        }

        return await query.OrderBy(c => c.FullName).ToListAsync();
    }
}
