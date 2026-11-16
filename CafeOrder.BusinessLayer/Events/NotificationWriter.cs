using CafeOrder.DataAccessLayer.Repositories.Notifications;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Events;

/// <summary>Bir siparişe bağlı bildirimi "eklenecek" işaretler.</summary>
public interface INotificationWriter
{
    /// <summary>Bildirimi hazırlar ve repository'ye ekler. Kayıt, servisin tek SaveChanges'inde yapılır.</summary>
    Task AddAsync(Order order, NotificationTarget target, string message);
}

/// <summary>
/// Observer'ların ortak yazma işi. Eskiden üç observer'da da aynı "new Notification + 5 satır" vardı;
/// artık observer'lar sadece hangi olaya, kime, hangi mesajı göndereceğine karar veriyor.
/// </summary>
public class NotificationWriter : INotificationWriter
{
    private readonly INotificationRepository _notificationRepository;
    private readonly TimeProvider _time;

    public NotificationWriter(INotificationRepository notificationRepository, TimeProvider time)
    {
        _notificationRepository = notificationRepository;
        _time = time;
    }

    /// <summary>Order navigation'ı ile bağlar: yeni siparişin Id'si henüz yok, EF kayıtta OrderId'yi doldurur.</summary>
    public async Task AddAsync(Order order, NotificationTarget target, string message)
    {
        Notification notification = new Notification
        {
            Order = order,
            Target = target,
            Message = message,
            CreatedAt = _time.GetLocalNow().DateTime
        };
        await _notificationRepository.AddAsync(notification);
    }
}
