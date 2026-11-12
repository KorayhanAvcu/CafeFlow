using CafeOrder.DataAccessLayer.Context;
using CafeOrder.DataAccessLayer.Repositories.GenericRepositories;
using CafeOrder.EntityLayer.Entities;

namespace CafeOrder.DataAccessLayer.Repositories.Tables;

public class TableRepository : GenericRepository<Table>, ITableRepository
{
    public TableRepository(CafeOrderDbContext context) : base(context)
    {
    }
}
