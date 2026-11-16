using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Services.Orders;

/// <summary>Sipariş okuma işleri: liste, detay, sayılar. Hiçbir şey değiştirmez.</summary>
public interface IOrderQueryService
{
    /// <summary>Admin sipariş listesi: durum, kanal ve aramaya göre filtreler, sayfa başına 10 sipariş, üstteki sayılar.</summary>
    Task<ResultOrderListDto> GetAdminListAsync(OrderFilterDto filter);
    /// <summary>Tek bir sipariş; kalemleri, seçenekleri, durum geçmişi ve izinli sonraki durumlarıyla. Yoksa null.</summary>
    Task<ResultOrderDto?> GetByIdAsync(int id);
    /// <summary>Sipariş numarasıyla sipariş (onay sayfası için). Yoksa null.</summary>
    Task<ResultOrderDto?> GetByOrderNoAsync(string orderNo);
    /// <summary>Belirli bir durumdaki sipariş sayısı (ör. menüdeki yeni sipariş rozeti).</summary>
    Task<int> CountByStatusAsync(OrderStatus status);
    /// <summary>Her durumdaki sipariş sayısı; siparişi olmayan durumlar 0.</summary>
    Task<Dictionary<OrderStatus, int>> GetStatusCountsAsync();
}
