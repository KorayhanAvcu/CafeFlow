using CafeOrder.DataAccessLayer.Repositories.GenericRepositories;
using CafeOrder.EntityLayer.Entities;

namespace CafeOrder.DataAccessLayer.Repositories.Tables;

public interface ITableRepository : IRepository<Table>
{
    /// <summary>Bütün masalar siparişleriyle (sayı için), numaraya göre.</summary>
    Task<List<Table>> GetAllWithOrdersAsync();
    /// <summary>Tek masa siparişleriyle; yoksa null.</summary>
    Task<Table?> GetWithOrdersAsync(int id);
    /// <summary>En büyük masa numarası; hiç masa yoksa 0.</summary>
    Task<int> GetMaxNumberAsync();
}
