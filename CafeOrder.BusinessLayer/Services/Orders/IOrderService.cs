using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Services.Orders;

public interface IOrderService
{
    /// <summary>Bütün siparişler, en yeni en üstte (admin listesi ve panel için).</summary>
    Task<List<ResultOrderDto>> GetAllAsync();
    /// <summary>Admin sipariş listesi: durum, kanal ve aramaya göre filtreler, sayfa başına 10 sipariş, üstteki sayılar.</summary>
    Task<ResultOrderListDto> GetAdminListAsync(OrderFilterDto filter);
    /// <summary>Admin panel sayfası: bugünkü sipariş ve ciro, açık siparişler, son 7 gün, durum sayıları, en çok satanlar, son 6 sipariş.</summary>
    Task<ResultDashboardDto> GetDashboardAsync();
    /// <summary>Tek bir sipariş; kalemleri, seçenekleri, durum geçmişi ve izinli sonraki durumlarıyla. Yoksa null.</summary>
    Task<ResultOrderDto?> GetByIdAsync(int id);
    /// <summary>Sipariş numarasıyla sipariş (onay sayfası için). Yoksa null.</summary>
    Task<ResultOrderDto?> GetByOrderNoAsync(string orderNo);
    /// <summary>Belirli bir durumdaki sipariş sayısı (ör. menüdeki yeni sipariş rozeti).</summary>
    Task<int> CountByStatusAsync(OrderStatus status);

    /// <summary>Sepeti fiyatlandırır (kaydetmez). Sepet sayfası ve sipariş aynı hesabı kullanır.</summary>
    Task<ResultOrderDto> CalculateAsync(OrderChannel channel, List<CartItemDto> items);

    /// <summary>Sepetten sipariş oluşturur: Factory Method → Decorator + Strategy → Chain of Responsibility → kayıt → Observer. Kural bozulursa BusinessException, hiçbir şey kaydedilmez.</summary>
    Task<ResultOrderDto> PlaceOrderAsync(CreateOrderDto dto, List<CartItemDto> items, string? qrToken);
    /// <summary>Siparişin durumunu değiştirir: State izin vermezse BusinessException; izin verirse geçmişe kayıt + Observer bildirimi.</summary>
    Task ChangeStatusAsync(int id, OrderStatus target, string? note);
}
