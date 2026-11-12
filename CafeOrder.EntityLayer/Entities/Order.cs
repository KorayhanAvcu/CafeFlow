using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.EntityLayer.Entities;

public class Order
{
    public int Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public OrderChannel Channel { get; set; }
    public OrderStatus Status { get; set; }

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? TableId { get; set; }
    public Table? Table { get; set; }

    public string? Address { get; set; }
    public string? AddressNotes { get; set; }
    public DateTime? PickupTime { get; set; }
    public string? Note { get; set; }

    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<OrderDetail> Details { get; set; } = new();
    public List<OrderStatusHistory> StatusHistories { get; set; } = new();
}