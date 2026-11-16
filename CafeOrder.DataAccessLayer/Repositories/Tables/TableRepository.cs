using CafeOrder.DataAccessLayer.Context;
using CafeOrder.DataAccessLayer.Repositories.GenericRepositories;
using CafeOrder.EntityLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafeOrder.DataAccessLayer.Repositories.Tables;

public class TableRepository : GenericRepository<Table>, ITableRepository
{
    public TableRepository(CafeOrderDbContext context) : base(context)
    {
    }

    /// <summary>Bütün masalar siparişleriyle, numaraya göre.</summary>
    public async Task<List<Table>> GetAllWithOrdersAsync()
    {
        return await Set
            .Include(t => t.Orders)
            .OrderBy(t => t.Number)
            .ToListAsync();
    }

    /// <summary>Tek masa siparişleriyle.</summary>
    public async Task<Table?> GetWithOrdersAsync(int id)
    {
        return await Set
            .Include(t => t.Orders)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    /// <summary>En büyük masa numarası (SQL MAX); tablo boşsa 0.</summary>
    public async Task<int> GetMaxNumberAsync()
    {
        return await Set.MaxAsync(t => (int?)t.Number) ?? 0;
    }
}
