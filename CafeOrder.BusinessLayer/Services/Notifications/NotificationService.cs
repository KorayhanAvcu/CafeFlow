using AutoMapper;
using CafeOrder.DataAccessLayer.Repositories.Notifications;
using CafeOrder.DataAccessLayer.UnitOfWork;
using CafeOrder.DtoLayer.Dtos.NotificationDtos;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Services.Notifications;

/// <summary>
/// Bildirimleri okur. Bildirimleri YAZAN taraf Events klasöründeki observer'lardır.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public NotificationService(INotificationRepository notificationRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    /// <summary>Bildirimler sayfası. target null ise hepsi; unreadOnly true ise sadece okunmamışlar.</summary>
    public async Task<List<ResultNotificationDto>> GetAllAsync(NotificationTarget? target, bool unreadOnly)
    {
        var notifications = await _notificationRepository.GetFilteredAsync(target, unreadOnly);
        return _mapper.Map<List<ResultNotificationDto>>(notifications);
    }

    /// <summary>
    /// Bildirimler sayfasının bütün verisi: filtrelenmiş liste, toplam okunmamış sayısı ve
    /// her hedef (mutfak, kasa, yönetici) için ayrı okunmamış sayısı.
    /// </summary>
    public async Task<ResultNotificationPageDto> GetPageAsync(NotificationTarget? target, bool unreadOnly)
    {
        ResultNotificationPageDto page = new ResultNotificationPageDto();
        page.Target = target;
        page.Notifications = await GetAllAsync(target, unreadOnly);
        page.UnreadAll = await CountUnreadAsync(null);
        foreach (NotificationTarget t in Enum.GetValues<NotificationTarget>())
        {
            page.UnreadByTarget[t] = await CountUnreadAsync(t);
        }
        return page;
    }

    /// <summary>Üst çubuktaki zil için son birkaç bildirim.</summary>
    public async Task<List<ResultNotificationDto>> GetLatestAsync(NotificationTarget target, int count)
    {
        var notifications = await _notificationRepository.GetLatestAsync(target, count);

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
        }
        // Hepsi tek SaveChanges ile (eskiden her bildirim için ayrı bir kayıt yapılıyordu).
        await _unitOfWork.SaveChangesAsync();
    }
}
