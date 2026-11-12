using CafeOrder.DataAccessLayer.Repositories.GenericRepositories;
using CafeOrder.EntityLayer.Entities;

namespace CafeOrder.DataAccessLayer.Repositories.Orders;

public interface IOrderRepository : IRepository<Order>
{
    /// <summary>Siparişi kalemleri ve ilk durum kaydıyla tek transaction içinde kaydeder.</summary>
    Task CreateWithTransactionAsync(Order order);
}
