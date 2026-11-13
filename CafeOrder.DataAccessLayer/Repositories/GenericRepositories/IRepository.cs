using System.Linq.Expressions;

namespace CafeOrder.DataAccessLayer.Repositories.GenericRepositories;

/// <summary>Her tablo için ortak işlemler: listele, bul, ekle, güncelle, sil.</summary>
public interface IRepository<TEntity> where TEntity : class
{
    /// <summary>Tablodaki bütün kayıtları getirir.</summary>
    Task<List<TEntity>> GetAllAsync();
    /// <summary>Id ile tek kayıt; yoksa null.</summary>
    Task<TEntity?> GetByIdAsync(int id);
    /// <summary>Yeni kaydı ekler ve hemen kaydeder.</summary>
    Task CreateAsync(TEntity entity);
    /// <summary>Kaydı (ve bağlı nesneleri) günceller ve kaydeder.</summary>
    Task UpdateAsync(TEntity entity);
    /// <summary>Kaydı siler ve kaydeder.</summary>
    Task DeleteAsync(TEntity entity);
    /// <summary>Koşula uyan bütün kayıtlar (koşul SQL WHERE'e çevrilir).</summary>
    Task<List<TEntity>> GetWhereAsync(Expression<Func<TEntity, bool>> predicate);
    /// <summary>Koşula uyan ilk kayıt; yoksa null.</summary>
    Task<TEntity?> GetFirstAsync(Expression<Func<TEntity, bool>> predicate);
    /// <summary>Koşula uyan en az bir kayıt var mı (SQL EXISTS).</summary>
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);
    /// <summary>Koşula uyan kayıt sayısı; koşul yoksa hepsi (SQL COUNT).</summary>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null);

    /// <summary>Include, OrderBy gibi şeyler eklemek için sorgunun kendisi.</summary>
    IQueryable<TEntity> GetQueryable();
}
