using AutoMapper;
using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

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

        // ---------- Sipariş oluştururken (entity'ler new'lenmez, buradan oluşur) ----------

        // Sepet fiyatlandırması için kanalı belli, geçici bir sipariş (OrderPricingService.CalculateAsync).
        CreateMap<OrderChannel, Order>()
            .ForMember(dest => dest.Channel, opt => opt.MapFrom(src => src));

        // Ürün → sipariş kalemi: o anki adın ve taban fiyatın kopyası.
        // Id İGNORE: yoksa Product.Id aynı isimli OrderDetail.Id'ye kopyalanır ve EF sabit Id ile INSERT etmeye çalışır.
        // Product navigation'ı İGNORE: servis AYNI izlenen ürün nesnesini bağlar (kopya ürün EF'e yeni ürün gibi görünür).
        CreateMap<Product, OrderDetail>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Product, opt => opt.Ignore())
            .ForMember(dest => dest.Options, opt => opt.Ignore());

        // Ürün seçeneği → kalem seçeneği: grup, değer ve fiyat farkının o anki kopyası. (Id yine İGNORE.)
        CreateMap<ProductOption, OrderDetailOption>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ProductOptionId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.ProductOption, opt => opt.Ignore());
    }
}
