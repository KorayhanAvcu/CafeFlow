using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.EntityLayer.Entities;

public class OrderStatusHistory
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public OrderStatus? FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Note { get; set; }
}