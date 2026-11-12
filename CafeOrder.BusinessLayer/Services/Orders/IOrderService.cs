using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Services.Orders;

public interface IOrderService
{
    Task<List<ResultOrderDto>> GetAllAsync();
    Task<ResultOrderDto?> GetByIdAsync(int id);
    Task<ResultOrderDto?> GetByOrderNoAsync(string orderNo);
    Task<int> CountByStatusAsync(OrderStatus status);

    /// <summary>Sepeti fiyatlandırır (kaydetmez). Sepet sayfası ve sipariş aynı hesabı kullanır.</summary>
    Task<ResultOrderDto> CalculateAsync(OrderChannel channel, List<CartItemDto> items);

    Task<ResultOrderDto> PlaceOrderAsync(CreateOrderDto dto, List<CartItemDto> items, string? qrToken);
    Task ChangeStatusAsync(int id, OrderStatus target, string? note);
}
