using AutoMapper;
using CafeOrder.DtoLayer.Dtos.NotificationDtos;
using CafeOrder.EntityLayer.Entities;

namespace CafeOrder.BusinessLayer.Mapping;

/// <summary>Bildirim eşlemeleri (AutoMapper Profile).</summary>
public class NotificationMappings : Profile
{
    public NotificationMappings()
    {
        // Bildirimin siparişi silinmiş olabilir; o zaman sipariş numarası boş kalır.
        CreateMap<Notification, ResultNotificationDto>()
            .ForMember(dest => dest.OrderNo, opt => opt.MapFrom(src => src.Order != null ? src.Order.OrderNo : null));
    }
}
