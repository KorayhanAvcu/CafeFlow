using AutoMapper;
using CafeOrder.DataAccessLayer.Repositories.Notifications;
using CafeOrder.DtoLayer.Dtos.NotificationDtos;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;
using Microsoft.EntityFrameworkCore;

namespace CafeOrder.BusinessLayer.Services.Notifications;

/// <summary>
/// Bildirimleri okur. Bildirimleri YAZAN taraf Events klasöründeki observer'lardır.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IMapper _mapper;

    public NotificationService(INotificationRepository notificationRepository, IMapper mapper)
    {
        _notificationRepository = notificationRepository;
        _mapper = mapper;
    }

    /// <summary>Bildirimler sayfası. target null ise hepsi; unreadOnly true ise sadece okunmamışlar.</summary>
    public async Task<List<ResultNotificationDto>> GetAllAsync(NotificationTarget? target, bool unreadOnly)
    {
        IQueryable<Notification> query = _notificationRepository.GetQueryable().Include(n => n.Order);

        if (target != null)
        {
            query = query.Where(n => n.Target == target);
        }
        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        List<Notification> notifications = await query.OrderByDescending(n => n.Id).ToListAsync();
        return _mapper.Map<List<ResultNotificationDto>>(notifications);
    }

    /// <summary>Üst çubuktaki zil için son birkaç bildirim.</summary>
    public async Task<List<ResultNotificationDto>> GetLatestAsync(NotificationTarget target, int count)
    {
        List<Notification> notifications = await _notificationRepository.GetQueryable()
            .Include(n => n.Order)
            .Where(n => n.Target == target)
            .OrderByDescending(n => n.Id)
            .Take(count)
            .ToListAsync();

        return _mapper.Map<List<ResultNotificationDto>>(notifications);
    }

    /// <summary>Okunmamış bildirim sayısı. target null ise hepsi sayılır.</summary>
    public async Task<int> CountUnreadAsync(NotificationTarget? target)
    {
        if (target == null)
        {
            return await _notificationRepository.CountAsync(n => !n.IsRead);
        }
        return await _notificationRepository.CountAsync(n => !n.IsRead && n.Target == target);
    }

    /// <summary>Okunmamış bildirimleri okundu yapar. target null ise hepsini.</summary>
    public async Task MarkAllReadAsync(NotificationTarget? target)
    {
        List<Notification> unread;
        if (target == null)
        {
            unread = await _notificationRepository.GetWhereAsync(n => !n.IsRead);
        }
        else
        {
            unread = await _notificationRepository.GetWhereAsync(n => !n.IsRead && n.Target == target);
        }

        foreach (Notification notification in unread)
        {
            notification.IsRead = true;
            await _notificationRepository.UpdateAsync(notification);
        }
    }
}
