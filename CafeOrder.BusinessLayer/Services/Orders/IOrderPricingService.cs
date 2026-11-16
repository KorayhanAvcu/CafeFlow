using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Services.Orders;

/// <summary>Sepet/sipariş fiyat hesabı (Decorator + Strategy). Sepet sayfası, sepet paneli ve sipariş kaydı aynı hesabı kullanır.</summary>
public interface IOrderPricingService
{
    /// <summary>Sepeti fiyatlandırır ama KAYDETMEZ (sepet sayfası ve sepet paneli).</summary>
    Task<ResultOrderDto> CalculateAsync(OrderChannel channel, List<CartItemDto> items);

    /// <summary>Siparişin kalemlerini sepetten kurar; birim fiyat, ara toplam, ücret ve toplamı hesaplar. Fiyatlar veritabanından okunur.</summary>
    Task ApplyPricesAsync(Order order, List<CartItemDto> items);
}
