using AutoMapper;
using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.EntityLayer.Entities;

namespace CafeOrder.BusinessLayer.Mapping;

/// <summary>Sipariş eşlemeleri (AutoMapper Profile).</summary>
public class OrderMappings : Profile
{
    public OrderMappings()
    {
        // Sipariş formu → Order. Kanal, adres, adres notu, alma saati ve not kopyalanır.
        // FullName ve Phone Order'da yok; OrderService müşteriyi ayrıca bulur/oluşturur.
        CreateMap<CreateOrderDto, Order>();

        // CustomerFullName, CustomerPhone, TableNumber, TableArea: flattening ile Customer.FullName, Table.Number ...
        // Masa siparişinde Customer, teslimatta Table null'dır; AutoMapper null'da hata vermez, alan boş kalır.
        // AllowedNextStatuses kaynakta yok; OrderQueryService State'e sorup ayrıca doldurur.
        CreateMap<Order, ResultOrderDto>();

        // ProductImageUrl, ProductCategoryName, ProductCategorySlug: flattening ile Product.ImageUrl, Product.Category.Name ...
        // (Kayıtlı sipariş okunurken Product Include edilmez; bu alanlar o zaman boş kalır, ProductName kopyası kullanılır.)
        CreateMap<OrderDetail, ResultOrderDetailDto>();

        CreateMap<OrderDetailOption, ResultOrderDetailOptionDto>();
        CreateMap<OrderStatusHistory, ResultOrderStatusHistoryDto>();
    }
}
