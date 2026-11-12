using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.DtoLayer.Dtos.NotificationDtos;

/// <summary>Bildirim listesi ve üst çubuktaki zil.</summary>
public class ResultNotificationDto
{
    public int Id { get; set; }
    public int? OrderId { get; set; }

    /// <summary>Bağlı siparişin numarası (Order.OrderNo).</summary>
    public string? OrderNo { get; set; }

    public NotificationTarget Target { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
