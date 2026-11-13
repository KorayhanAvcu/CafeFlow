using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.DtoLayer.Dtos.OrderDtos;

/// <summary>
/// Admin sipariş listesinin filtreleri. Adres çubuğundan gelir: /admin/orders?status=Received&amp;channel=Table&amp;q=CF-12&amp;page=2
/// </summary>
public class OrderFilterDto
{
    public OrderStatus? Status { get; set; }
    public OrderChannel? Channel { get; set; }
    public string? Q { get; set; }
    public int Page { get; set; } = 1;
}
