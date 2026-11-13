using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.DtoLayer.Dtos.NotificationDtos;

/// <summary>Admin bildirimler sayfasının bütün verisi. NotificationService.GetPageAsync hazırlar.</summary>
public class ResultNotificationPageDto
{
    public List<ResultNotificationDto> Notifications { get; set; } = new List<ResultNotificationDto>();

    /// <summary>Seçili hedef filtresi (null = hepsi).</summary>
    public NotificationTarget? Target { get; set; }

    /// <summary>Okunmamış bildirim sayısı (bütün hedefler).</summary>
    public int UnreadAll { get; set; }

    /// <summary>Hedef başına okunmamış bildirim sayısı.</summary>
    public Dictionary<NotificationTarget, int> UnreadByTarget { get; set; } = new Dictionary<NotificationTarget, int>();
}
