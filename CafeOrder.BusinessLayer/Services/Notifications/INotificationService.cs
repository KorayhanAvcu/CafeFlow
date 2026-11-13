using CafeOrder.DtoLayer.Dtos.NotificationDtos;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Services.Notifications;

public interface INotificationService
{
    /// <summary>Bildirim listesi; hedefe (mutfak/kasa/yönetici) ve okunmamışlara göre filtrelenebilir.</summary>
    Task<List<ResultNotificationDto>> GetAllAsync(NotificationTarget? target, bool unreadOnly);
    /// <summary>Bildirimler sayfasının bütün verisi: filtrelenmiş liste ve hedef başına okunmamış sayıları.</summary>
    Task<ResultNotificationPageDto> GetPageAsync(NotificationTarget? target, bool unreadOnly);
    /// <summary>Bir hedefin en son birkaç bildirimi (üst çubuktaki zil için).</summary>
    Task<List<ResultNotificationDto>> GetLatestAsync(NotificationTarget target, int count);
    /// <summary>Okunmamış bildirim sayısı; hedef null ise hepsi.</summary>
    Task<int> CountUnreadAsync(NotificationTarget? target);
    /// <summary>Okunmamış bildirimleri okundu yapar; hedef null ise hepsini.</summary>
    Task MarkAllReadAsync(NotificationTarget? target);
}
