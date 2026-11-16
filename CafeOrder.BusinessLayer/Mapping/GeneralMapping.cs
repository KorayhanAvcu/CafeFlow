using CafeOrder.DtoLayer.Dtos.CategoryDtos;
using CafeOrder.DtoLayer.Dtos.CustomerDtos;
using CafeOrder.DtoLayer.Dtos.NotificationDtos;
using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.DtoLayer.Dtos.ProductDtos;
using CafeOrder.DtoLayer.Dtos.ProductOptionDtos;
using CafeOrder.DtoLayer.Dtos.TableDtos;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;
using Mapster;

namespace CafeOrder.BusinessLayer.Mapping;

/// <summary>
/// Mapster eşlemeleri: Entity ↔ DTO. (Mapster ücretsiz ve açık kaynaktır, MIT lisanslı.)
/// Aynı isimli alanlar kendiliğinden kopyalanır (Name → Name).
/// "CategoryName" gibi birleşik isimler de kendiliğinden bulunur (Category.Name → CategoryName; flattening).
/// İsmi tutmayan ya da hesaplanan alanlar .Map(...) ile elle yazılır.
/// Burada sadece özel kuralı olan eşlemeler yazılır; kuralı olmayanları (ör. Product → ResultProductDto) Mapster kendisi çıkarır.
/// BusinessServiceRegistration'da TypeAdapterConfig taranarak (Scan) tanıtılır.
/// </summary>
public class GeneralMapping : IRegister
{
    /// <summary>Özel kuralı olan eşlemeleri Mapster ayarlarına ekler. Uygulama açılırken bir kez çalışır.</summary>
    public void Register(TypeAdapterConfig config)
    {
        // ---------- Kategori ----------
        // Ürün sayısı entity'de alan olarak yok; Products listesinden sayılır.
        config.NewConfig<Category, ResultCategoryDto>()
            .Map(dest => dest.ProductCount, src => src.Products.Count);

        // ---------- Masa ----------
        config.NewConfig<Table, ResultTableDto>()
            .Map(dest => dest.OrderCount, src => src.Orders.Count);

        // ---------- Müşteri ----------
        // Sipariş sayısı, iptal hariç toplam harcama ve son sipariş tarihi müşterinin siparişlerinden hesaplanır.
        config.NewConfig<Customer, ResultCustomerDto>()
            .Map(dest => dest.OrderCount, src => src.Orders.Count)
            .Map(dest => dest.TotalSpent, src => src.Orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.Total))
            .Map(dest => dest.LastOrderDate, src => src.Orders.Count == 0 ? (DateTime?)null : src.Orders.Max(o => o.CreatedAt));

        // ---------- Bildirim ----------
        // Bildirimin siparişi silinmiş olabilir; o zaman sipariş numarası boş kalır.
        config.NewConfig<Notification, ResultNotificationDto>()
            .Map(dest => dest.OrderNo, src => src.Order != null ? src.Order.OrderNo : null);

        // Kuralsız (kendiliğinden) eşlemeler, okuyana fikir versin diye:
        //   Category ↔ UpdateCategoryDto, CreateCategoryDto → Category, UpdateCategoryDto → CreateCategoryDto
        //   Product → ResultProductDto (CategoryName, CategorySlug, CategoryIcon flattening ile), ProductOption → ResultProductOptionDto
        //   Product ↔ UpdateProductDto, Table ↔ UpdateTableDto, Create*Dto → entity
        //   Order → ResultOrderDto (CustomerFullName, TableNumber ... flattening ile), OrderDetail → ResultOrderDetailDto,
        //   OrderDetailOption → ResultOrderDetailOptionDto, OrderStatusHistory → ResultOrderStatusHistoryDto, CreateOrderDto → Order
    }
}
