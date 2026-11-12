using CafeOrder.DataAccessLayer.Context;
using CafeOrder.DataAccessLayer.Repositories.GenericRepositories;
using CafeOrder.EntityLayer.Entities;

namespace CafeOrder.DataAccessLayer.Repositories.Orders;

public class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    public OrderRepository(CafeOrderDbContext context) : base(context)
    {
    }

    public async Task CreateWithTransactionAsync(Order order)
    {
        // Ya hepsi yazılır ya hiçbiri: sipariş, kalemler, seçenekler, durum geçmişi, yeni müşteri.
        await using var transaction = await _context.Database.BeginTransactionAsync();
        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
