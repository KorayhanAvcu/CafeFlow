namespace CafeOrder.DataAccessLayer.UnitOfWork;

/// <summary>
/// UNIT OF WORK: bir istekteki bütün repository değişiklikleri tek seferde, tek transaction'da yazılır.
/// Repository'ler aynı (scoped) DbContext'i paylaştığı için hepsinin değişikliği buraya toplanır.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>İşaretlenen bütün eklemeleri, güncellemeleri ve silmeleri veritabanına yazar. Ya hepsi yazılır ya hiçbiri.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
