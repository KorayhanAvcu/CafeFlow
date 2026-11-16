using CafeOrder.DataAccessLayer.Context;
using CafeOrder.DataAccessLayer.Repositories.GenericRepositories;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;
using Microsoft.EntityFrameworkCore;

namespace CafeOrder.DataAccessLayer.Repositories.Orders;

public class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    public OrderRepository(CafeOrderDbContext context) : base(context)
    {
    }

    /// <summary>Id ile siparişi bütün ayrıntılarıyla getirir.</summary>
    public async Task<Order?> GetWithDetailsAsync(int id)
    {
        return await WithDetails().FirstOrDefaultAsync(o => o.Id == id);
    }

    /// <summary>Sipariş numarasıyla siparişi bütün ayrıntılarıyla getirir.</summary>
    public async Task<Order?> GetWithDetailsByOrderNoAsync(string orderNo)
    {
        return await WithDetails().FirstOrDefaultAsync(o => o.OrderNo == orderNo);
    }

    /// <summary>Durumlara göre sayılar tek SQL GROUP BY sorgusuyla.</summary>
    public async Task<Dictionary<OrderStatus, int>> GetStatusCountsAsync()
    {
        var rows = await Set
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return rows.ToDictionary(r => r.Status, r => r.Count);
    }

    /// <summary>Filtreye uyan sipariş sayısı.</summary>
    public async Task<int> CountFilteredAsync(OrderStatus? status, OrderChannel? channel, string? search)
    {
        return await Filter(Set, status, channel, search).CountAsync();
    }

    /// <summary>Filtreye uyan siparişlerin bir sayfası; liste için müşteri, masa ve kalemleriyle.</summary>
    public async Task<List<Order>> GetFilteredPageAsync(OrderStatus? status, OrderChannel? channel, string? search, int skip, int take)
    {
        return await Filter(ForList(), status, channel, search)
            .OrderByDescending(o => o.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    /// <summary>En yeni siparişler.</summary>
    public async Task<List<Order>> GetLatestAsync(int count)
    {
        return await ForList()
            .OrderByDescending(o => o.Id)
            .Take(count)
            .ToListAsync();
    }

    /// <summary>Verilen tarihten sonra verilen sipariş sayısı.</summary>
    public async Task<int> CountSinceAsync(DateTime from)
    {
        return await Set.CountAsync(o => o.CreatedAt >= from);
    }

    /// <summary>Verilen tarihten sonraki ciro; iptal edilen siparişler sayılmaz.</summary>
    public async Task<decimal> GetRevenueSinceAsync(DateTime from)
    {
        return await Set
            .Where(o => o.CreatedAt >= from && o.Status != OrderStatus.Cancelled)
            .SumAsync(o => o.Total);
    }

    /// <summary>İptal edilmemiş siparişlerin kalemleri ürün adına göre toplanır, adede göre sıralanır.</summary>
    public async Task<List<ProductSales>> GetBestSellersAsync(int count)
    {
        var rows = await _context.OrderDetails
            .Where(d => d.Order.Status != OrderStatus.Cancelled)
            .GroupBy(d => d.ProductName)
            .Select(g => new { Name = g.Key, Quantity = g.Sum(d => d.Quantity), Revenue = g.Sum(d => d.LineTotal) })
            .OrderByDescending(r => r.Quantity)
            .Take(count)
            .ToListAsync();

        return rows.Select(r => new ProductSales(r.Name, r.Quantity, r.Revenue)).ToList();
    }

    /// <summary>Ürün herhangi bir sipariş kaleminde geçiyor mu (SQL EXISTS)?</summary>
    public async Task<bool> AnyContainingProductAsync(int productId)
    {
        return await _context.OrderDetails.AnyAsync(d => d.ProductId == productId);
    }

    /// <summary>Detay sayfası için: müşteri, masa, kalemler, seçenekler ve durum geçmişi.</summary>
    private IQueryable<Order> WithDetails()
    {
        return Set
            .Include(o => o.Customer)
            .Include(o => o.Table)
            .Include(o => o.Details).ThenInclude(d => d.Options)
            .Include(o => o.StatusHistories);
    }

    /// <summary>Liste satırları için: müşteri, masa ve kalemler.</summary>
    private IQueryable<Order> ForList()
    {
        return Set
            .Include(o => o.Customer)
            .Include(o => o.Table)
            .Include(o => o.Details);
    }

    /// <summary>Durum, kanal ve arama (sipariş no, müşteri adı, telefon) filtresi. Büyük/küçük harf duyarsızlığı SQL Server'ın varsayılan collation'ından gelir.</summary>
    private static IQueryable<Order> Filter(IQueryable<Order> query, OrderStatus? status, OrderChannel? channel, string? search)
    {
        if (status != null)
        {
            query = query.Where(o => o.Status == status);
        }
        if (channel != null)
        {
            query = query.Where(o => o.Channel == channel);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            string q = search.Trim();
            query = query.Where(o =>
                o.OrderNo.Contains(q)
                || (o.Customer != null && (o.Customer.FullName.Contains(q) || o.Customer.Phone.Contains(q))));
        }
        return query;
    }
}
