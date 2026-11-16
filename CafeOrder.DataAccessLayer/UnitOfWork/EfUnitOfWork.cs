using CafeOrder.DataAccessLayer.Context;

namespace CafeOrder.DataAccessLayer.UnitOfWork;

/// <summary>EF Core ile Unit of Work: SaveChanges zaten tek transaction içinde çalışır.</summary>
public class EfUnitOfWork : IUnitOfWork
{
    private readonly CafeOrderDbContext _context;

    public EfUnitOfWork(CafeOrderDbContext context)
    {
        _context = context;
    }

    /// <summary>Bu istekteki bütün değişiklikleri tek SaveChanges ile yazar.</summary>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
