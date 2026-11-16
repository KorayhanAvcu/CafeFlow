using System.Linq.Expressions;
using CafeOrder.DataAccessLayer.Context;
using Microsoft.EntityFrameworkCore;

namespace CafeOrder.DataAccessLayer.Repositories.GenericRepositories;

public class GenericRepository<TEntity> : IRepository<TEntity> where TEntity : class
{
    protected readonly CafeOrderDbContext _context;

    public GenericRepository(CafeOrderDbContext context)
    {
        _context = context;
    }

    /// <summary>Bu tablonun sorgusu. Sadece repository'lerin içinde kullanılır; servislere IQueryable verilmez.</summary>
    protected DbSet<TEntity> Set => _context.Set<TEntity>();

    /// <summary>Tablodaki bütün kayıtları listeye çeker.</summary>
    public async Task<List<TEntity>> GetAllAsync()
    {
        return await Set.ToListAsync();
    }

    /// <summary>Id ile kaydı bulur; önce bu istekte zaten yüklenmiş mi diye bakar (FindAsync).</summary>
    public async Task<TEntity?> GetByIdAsync(int id)
    {
        return await Set.FindAsync(id);
    }

    /// <summary>Kaydı "eklenecek" işaretler. INSERT, IUnitOfWork.SaveChangesAsync çağrılınca yapılır; Id o zaman dolar.</summary>
    public async Task AddAsync(TEntity entity)
    {
        await Set.AddAsync(entity);
    }

    /// <summary>Kaydı "değişti" işaretler. Bağlı yeni nesneler (Id = 0) "eklenecek" olur.</summary>
    public void Update(TEntity entity)
    {
        Set.Update(entity);
    }

    /// <summary>Kaydı "silinecek" işaretler.</summary>
    public void Remove(TEntity entity)
    {
        Set.Remove(entity);
    }

    /// <summary>Koşulu SQL WHERE'e çevirip uyan kayıtları getirir.</summary>
    public async Task<List<TEntity>> GetWhereAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await Set.Where(predicate).ToListAsync();
    }

    /// <summary>Koşula uyan ilk kaydı getirir (SQL TOP 1); yoksa null.</summary>
    public async Task<TEntity?> GetFirstAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await Set.FirstOrDefaultAsync(predicate);
    }

    /// <summary>Koşula uyan kayıt var mı diye bakar (SQL EXISTS); kayıtları çekmez.</summary>
    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await Set.AnyAsync(predicate);
    }

    /// <summary>Kayıt sayısını SQL COUNT ile döndürür; koşul verilmezse bütün tablo.</summary>
    public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null)
    {
        if (predicate is null)
        {
            return await Set.CountAsync();
        }
        return await Set.CountAsync(predicate);
    }
}
