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

    /// <summary>Tablodaki bütün kayıtları listeye çeker.</summary>
    public async Task<List<TEntity>> GetAllAsync()
    {
        return await _context.Set<TEntity>().ToListAsync();
    }

    /// <summary>Id ile kaydı bulur; önce bu istekte zaten yüklenmiş mi diye bakar (FindAsync).</summary>
    public async Task<TEntity?> GetByIdAsync(int id)
    {
        return await _context.Set<TEntity>().FindAsync(id);
    }

    /// <summary>Kaydı "eklenecek" işaretler ve SaveChanges ile INSERT eder. Sonrasında entity.Id dolu olur.</summary>
    public async Task CreateAsync(TEntity entity)
    {
        await _context.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    /// <summary>Kaydı "değişti" işaretler ve SaveChanges ile UPDATE eder. Bağlı yeni nesneler (Id = 0) INSERT edilir.</summary>
    public async Task UpdateAsync(TEntity entity)
    {
        _context.Update(entity);
        await _context.SaveChangesAsync();
    }

    /// <summary>Kaydı "silinecek" işaretler ve SaveChanges ile DELETE eder.</summary>
    public async Task DeleteAsync(TEntity entity)
    {
        _context.Remove(entity);
        await _context.SaveChangesAsync();
    }

    /// <summary>Koşulu SQL WHERE'e çevirip uyan kayıtları getirir.</summary>
    public async Task<List<TEntity>> GetWhereAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await _context.Set<TEntity>().Where(predicate).ToListAsync();
    }

    /// <summary>Koşula uyan ilk kaydı getirir (SQL TOP 1); yoksa null.</summary>
    public async Task<TEntity?> GetFirstAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await _context.Set<TEntity>().FirstOrDefaultAsync(predicate);
    }

    /// <summary>Koşula uyan kayıt var mı diye bakar (SQL EXISTS); kayıtları çekmez.</summary>
    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await _context.Set<TEntity>().AnyAsync(predicate);
    }

    /// <summary>Kayıt sayısını SQL COUNT ile döndürür; koşul verilmezse bütün tablo.</summary>
    public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null)
    {
        if (predicate is null)
            return await _context.Set<TEntity>().CountAsync();

        return await _context.Set<TEntity>().CountAsync(predicate);
    }

    /// <summary>Henüz çalışmamış sorguyu döndürür; servisler üstüne Include/Where/OrderBy ekler, ToListAsync ile tek SQL çalışır.</summary>
    public IQueryable<TEntity> GetQueryable()
    {
        return _context.Set<TEntity>().AsQueryable();
    }
}
