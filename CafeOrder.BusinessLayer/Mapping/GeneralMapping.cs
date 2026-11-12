using AutoMapper;
using CafeOrder.DtoLayer.Dtos.CategoryDtos;
using CafeOrder.DtoLayer.Dtos.CustomerDtos;
using CafeOrder.DtoLayer.Dtos.NotificationDtos;
using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.DtoLayer.Dtos.ProductDtos;
using CafeOrder.DtoLayer.Dtos.ProductOptionDtos;
using CafeOrder.DtoLayer.Dtos.TableDtos;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Mapping;

/// <summary>
/// AutoMapper eşlemeleri: Entity ↔ DTO.
/// Aynı isimli alanlar kendiliğinden kopyalanır (Name → Name).
/// "CategoryName" gibi birleşik isimler de kendiliğinden bulunur (Category.Name → CategoryName).
/// İsmi tutmayan ya da hesaplanan alanlar ForMember ile elle yazılır.
/// Program.cs'te builder.Services.AddAutoMapper(typeof(GeneralMapping)) ile tanıtılır.
/// </summary>
public class GeneralMapping : Profile
{
    public GeneralMapping()
    {
        // ---------- Kategori ----------
        CreateMap<Category, ResultCategoryDto>()
            .ForMember(dest => dest.ProductCount, opt => opt.MapFrom(src => src.Products.Count));
        CreateMap<CreateCategoryDto, Category>();
        CreateMap<UpdateCategoryDto, Category>();
        CreateMap<ResultCategoryDto, UpdateCategoryDto>();    // düzenleme formunu doldurmak için
        CreateMap<UpdateCategoryDto, CreateCategoryDto>();    // form Id = 0 ile gelirse yeni kayıt

        // ---------- Ürün ----------
        CreateMap<Product, ResultProductDto>();               // CategoryName, CategorySlug, CategoryIcon kendiliğinden
        CreateMap<CreateProductDto, Product>();
        CreateMap<UpdateProductDto, Product>();
        CreateMap<ResultProductDto, UpdateProductDto>();
        CreateMap<UpdateProductDto, CreateProductDto>();
        CreateMap<ProductOption, ResultProductOptionDto>();

        // ---------- Masa ----------
        CreateMap<Table, ResultTableDto>()
            .ForMember(dest => dest.OrderCount, opt => opt.MapFrom(src => src.Orders.Count));
        CreateMap<CreateTableDto, Table>();
        CreateMap<UpdateTableDto, Table>();
        CreateMap<ResultTableDto, UpdateTableDto>();
        CreateMap<UpdateTableDto, CreateTableDto>();

        // ---------- Müşteri ----------
        CreateMap<Customer, ResultCustomerDto>()
            .ForMember(dest => dest.OrderCount, opt => opt.MapFrom(src => src.Orders.Count))
            .ForMember(dest => dest.TotalSpent, opt => opt.MapFrom(src =>
                src.Orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.Total)))
            .ForMember(dest => dest.LastOrderDate, opt => opt.MapFrom(src =>
                src.Orders.Count == 0 ? (DateTime?)null : src.Orders.Max(o => o.CreatedAt)));

        // ---------- Bildirim ----------
        CreateMap<Notification, ResultNotificationDto>()
            .ForMember(dest => dest.OrderNo, opt => opt.MapFrom(src => src.Order != null ? src.Order.OrderNo : null));

        // ---------- Sipariş ----------
        CreateMap<Order, ResultOrderDto>();                   // CustomerFullName, TableNumber ... kendiliğinden
        CreateMap<OrderDetail, ResultOrderDetailDto>();       // ProductImageUrl, ProductCategorySlug ... kendiliğinden
        CreateMap<OrderDetailOption, ResultOrderDetailOptionDto>();
        CreateMap<OrderStatusHistory, ResultOrderStatusHistoryDto>();

        // Sipariş formu → Order. Müşteri (ad/telefon) OrderService'te ayrıca bulunur ya da oluşturulur.
        CreateMap<CreateOrderDto, Order>();
    }
}
