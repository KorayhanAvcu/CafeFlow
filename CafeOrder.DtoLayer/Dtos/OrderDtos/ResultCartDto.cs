using CafeOrder.DtoLayer.Dtos.TableDtos;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.DtoLayer.Dtos.OrderDtos;

/// <summary>
/// Sepet sayfasının bütün verisi. CartService.GetCartAsync hazırlar; controller sadece view'a verir.
/// </summary>
public class ResultCartDto
{
    /// <summary>Fiyatlandırılmış sepet: kalemler, ara toplam, ücret, toplam (OrderPricingService hesaplar).</summary>
    public ResultOrderDto Order { get; set; } = new ResultOrderDto();

    /// <summary>Silinmiş ürünlerden temizlenmiş sepet; controller bunu Session'a geri yazar.</summary>
    public List<CartItemDto> Items { get; set; } = new List<CartItemDto>();

    /// <summary>Seçilen sipariş türü; henüz seçilmediyse null.</summary>
    public OrderChannel? Channel { get; set; }

    /// <summary>QR ile okutulan masa; yoksa null.</summary>
    public ResultTableDto? Table { get; set; }

    /// <summary>Formda yazılanlar (hata olunca kaybolmasın diye geri gönderilir).</summary>
    public CreateOrderDto Form { get; set; } = new CreateOrderDto();

    /// <summary>Gösterilecek hata mesajı; yoksa null.</summary>
    public string? Error { get; set; }

    public decimal FreeDeliveryFrom { get; set; }
}
