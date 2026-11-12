using CafeOrder.DtoLayer.Dtos.NotificationDtos;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Services.Notifications;

public interface INotificationService
{
    Task<List<ResultNotificationDto>> GetAllAsync(NotificationTarget? target, bool unreadOnly);
    Task<List<ResultNotificationDto>> GetLatestAsync(NotificationTarget target, int count);
    Task<int> CountUnreadAsync(NotificationTarget? target);
    Task MarkAllReadAsync(NotificationTarget? target);
}
