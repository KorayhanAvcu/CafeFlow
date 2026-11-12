using System.Linq.Expressions;

namespace CafeOrder.DataAccessLayer.Repositories.GenericRepositories;

/// <summary>Her tablo için ortak işlemler: listele, bul, ekle, güncelle, sil.</summary>
public interface IRepository<TEntity> where TEntity : class
{
    Task<List<TEntity>> GetAllAsync();
    Task<TEntity?> GetByIdAsync(int id);
    Task CreateAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(TEntity entity);
    Task<List<TEntity>> GetWhereAsync(Expression<Func<TEntity, bool>> predicate);
    Task<TEntity?> GetFirstAsync(Expression<Func<TEntity, bool>> predicate);
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null);

    /// <summary>Include, OrderBy gibi şeyler eklemek için sorgunun kendisi.</summary>
    IQueryable<TEntity> GetQueryable();
}
