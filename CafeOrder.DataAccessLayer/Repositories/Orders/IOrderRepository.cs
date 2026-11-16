using CafeOrder.DataAccessLayer.Repositories.GenericRepositories;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.DataAccessLayer.Repositories.Orders;

public interface IOrderRepository : IRepository<Order>
{
    /// <summary>Sipariş; müşteri, masa, kalemler, seçenekler ve durum geçmişiyle. Yoksa null.</summary>
    Task<Order?> GetWithDetailsAsync(int id);
    /// <summary>Sipariş numarasıyla sipariş; müşteri, masa, kalemler, seçenekler ve durum geçmişiyle. Yoksa null.</summary>
    Task<Order?> GetWithDetailsByOrderNoAsync(string orderNo);

    /// <summary>Her durumdaki sipariş sayısı (SQL GROUP BY). Hiç siparişi olmayan durum listede yoktur.</summary>
    Task<Dictionary<OrderStatus, int>> GetStatusCountsAsync();
    /// <summary>Filtreye uyan sipariş sayısı (SQL COUNT).</summary>
    Task<int> CountFilteredAsync(OrderStatus? status, OrderChannel? channel, string? search);
    /// <summary>Filtreye uyan siparişlerin bir sayfası, yeniden eskiye (SQL OFFSET/FETCH).</summary>
    Task<List<Order>> GetFilteredPageAsync(OrderStatus? status, OrderChannel? channel, string? search, int skip, int take);

    /// <summary>En yeni siparişler; müşteri, masa ve kalemleriyle.</summary>
    Task<List<Order>> GetLatestAsync(int count);
    /// <summary>Verilen tarihten sonra verilen sipariş sayısı (iptaller dahil).</summary>
    Task<int> CountSinceAsync(DateTime from);
    /// <summary>Verilen tarihten sonraki ciro (iptaller hariç, SQL SUM).</summary>
    Task<decimal> GetRevenueSinceAsync(DateTime from);
    /// <summary>En çok satan ürünler (iptaller hariç), adede göre.</summary>
    Task<List<ProductSales>> GetBestSellersAsync(int count);

    /// <summary>Ürün herhangi bir siparişte geçiyor mu?</summary>
    Task<bool> AnyContainingProductAsync(int productId);
}
