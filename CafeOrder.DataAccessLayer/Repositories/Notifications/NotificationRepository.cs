using CafeOrder.DataAccessLayer.Context;
using CafeOrder.DataAccessLayer.Repositories.GenericRepositories;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;
using Microsoft.EntityFrameworkCore;

namespace CafeOrder.DataAccessLayer.Repositories.Notifications;

public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
{
    public NotificationRepository(CafeOrderDbContext context) : base(context)
    {
    }

    /// <summary>Hedefe ve okunma durumuna göre filtrelenmiş bildirimler.</summary>
    public async Task<List<Notification>> GetFilteredAsync(NotificationTarget? target, bool unreadOnly)
    {
        IQueryable<Notification> query = Set.Include(n => n.Order);

        if (target != null)
        {
            query = query.Where(n => n.Target == target);
        }
        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query.OrderByDescending(n => n.Id).ToListAsync();
    }

    /// <summary>Bir hedefin en yeni bildirimleri.</summary>
    public async Task<List<Notification>> GetLatestAsync(NotificationTarget target, int count)
    {
        return await Set
            .Include(n => n.Order)
            .Where(n => n.Target == target)
            .OrderByDescending(n => n.Id)
            .Take(count)
            .ToListAsync();
    }
}
