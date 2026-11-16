using CafeOrder.DataAccessLayer.Repositories.GenericRepositories;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.DataAccessLayer.Repositories.Notifications;

public interface INotificationRepository : IRepository<Notification>
{
    /// <summary>Bildirimler siparişleriyle, yeniden eskiye. target null ise hepsi; unreadOnly true ise sadece okunmamışlar.</summary>
    Task<List<Notification>> GetFilteredAsync(NotificationTarget? target, bool unreadOnly);
    /// <summary>Bir hedefin en yeni bildirimleri, siparişleriyle.</summary>
    Task<List<Notification>> GetLatestAsync(NotificationTarget target, int count);
}
