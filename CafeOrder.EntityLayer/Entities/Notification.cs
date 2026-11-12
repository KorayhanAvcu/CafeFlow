using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.EntityLayer.Entities;

public class Notification
{
    public int Id { get; set; }
    public int? OrderId { get; set; }
    public Order? Order { get; set; }
    public NotificationTarget Target { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}