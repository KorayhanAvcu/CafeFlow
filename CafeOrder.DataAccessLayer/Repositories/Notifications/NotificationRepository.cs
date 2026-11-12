using CafeOrder.DataAccessLayer.Context;
using CafeOrder.DataAccessLayer.Repositories.GenericRepositories;
using CafeOrder.EntityLayer.Entities;

namespace CafeOrder.DataAccessLayer.Repositories.Notifications;

public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
{
    public NotificationRepository(CafeOrderDbContext context) : base(context)
    {
    }
}
