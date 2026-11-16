using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Services.Orders;

/// <summary>
/// Siparişi değiştiren işler: sipariş verme ve durum değiştirme.
/// Okuma IOrderQueryService'te, fiyat hesabı IOrderPricingService'te, panel rakamları IDashboardService'te.
/// </summary>
public interface IOrderService
{
    /// <summary>Sepetten sipariş oluşturur: Factory Method → Decorator + Strategy → Chain of Responsibility → Observer → tek kayıt. Kural bozulursa BusinessException, hiçbir şey kaydedilmez.</summary>
    Task<ResultOrderDto> PlaceOrderAsync(CreateOrderDto dto, List<CartItemDto> items, string? qrToken);
    /// <summary>Siparişin durumunu değiştirir: State izin vermezse BusinessException; izin verirse geçmişe kayıt + Observer bildirimi, tek kayıt.</summary>
    Task ChangeStatusAsync(int id, OrderStatus target, string? note);
}
