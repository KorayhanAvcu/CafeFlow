using AutoMapper;
using CafeOrder.DtoLayer.Dtos.CustomerDtos;
using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Mapping;

/// <summary>Müşteri eşlemeleri (AutoMapper Profile). Müşteriler sipariş verirken oluşur; admin sadece listeler.</summary>
public class CustomerMappings : Profile
{
    public CustomerMappings()
    {
        // Sipariş sayısı, iptal hariç toplam harcama ve son sipariş tarihi müşterinin siparişlerinden hesaplanır.
        CreateMap<Customer, ResultCustomerDto>()
            .ForMember(dest => dest.OrderCount, opt => opt.MapFrom(src => src.Orders.Count))
            .ForMember(dest => dest.TotalSpent, opt => opt.MapFrom(src =>
                src.Orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.Total)))
            .ForMember(dest => dest.LastOrderDate, opt => opt.MapFrom(src =>
                src.Orders.Count == 0 ? (DateTime?)null : src.Orders.Max(o => o.CreatedAt)));

        // Sipariş formu → yeni müşteri (telefonla kayıtlı müşteri yoksa). Ad ve telefon kırpılır.
        CreateMap<CreateOrderDto, Customer>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => (src.FullName ?? "").Trim()))
            .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => (src.Phone ?? "").Trim()));
    }
}
