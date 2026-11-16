using System.Linq.Expressions;

namespace CafeOrder.DataAccessLayer.Repositories.GenericRepositories;

/// <summary>
/// Her tablo için ortak işlemler: listele, bul, ekle, güncelle, sil.
/// Ekleme/güncelleme/silme sadece "işaretler"; veritabanına yazmak IUnitOfWork.SaveChangesAsync'in işidir.
/// Böylece bir iş (ör. sipariş + bildirimler) tek SaveChanges ile, tek transaction'da yazılır.
/// </summary>
public interface IRepository<TEntity> where TEntity : class
{
    /// <summary>Tablodaki bütün kayıtları getirir.</summary>
    Task<List<TEntity>> GetAllAsync();
    /// <summary>Id ile tek kayıt; yoksa null.</summary>
    Task<TEntity?> GetByIdAsync(int id);
    /// <summary>Yeni kaydı "eklenecek" işaretler (INSERT, SaveChanges'te).</summary>
    Task AddAsync(TEntity entity);
    /// <summary>Kaydı (ve bağlı nesneleri) "değişti" işaretler (UPDATE, SaveChanges'te).</summary>
    void Update(TEntity entity);
    /// <summary>Kaydı "silinecek" işaretler (DELETE, SaveChanges'te).</summary>
    void Remove(TEntity entity);
    /// <summary>Koşula uyan bütün kayıtlar (koşul SQL WHERE'e çevrilir).</summary>
    Task<List<TEntity>> GetWhereAsync(Expression<Func<TEntity, bool>> predicate);
    /// <summary>Koşula uyan ilk kayıt; yoksa null.</summary>
    Task<TEntity?> GetFirstAsync(Expression<Func<TEntity, bool>> predicate);
    /// <summary>Koşula uyan en az bir kayıt var mı (SQL EXISTS).</summary>
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);
    /// <summary>Koşula uyan kayıt sayısı; koşul yoksa hepsi (SQL COUNT).</summary>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null);
}
