using CafeOrder.DataAccessLayer.Repositories.GenericRepositories;
using CafeOrder.EntityLayer.Entities;

namespace CafeOrder.DataAccessLayer.Repositories.Customers;

public interface ICustomerRepository : IRepository<Customer>
{
    /// <summary>Müşteriler siparişleriyle, ada göre. search verilirse ad veya telefonda arar.</summary>
    Task<List<Customer>> SearchWithOrdersAsync(string? search);
}
