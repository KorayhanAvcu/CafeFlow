using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.DtoLayer.Dtos.OrderDtos;

/// <summary>Sipariş durum geçmişinin bir satırı.</summary>
public class ResultOrderStatusHistoryDto
{
    public int Id { get; set; }
    public OrderStatus? FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Note { get; set; }
}
