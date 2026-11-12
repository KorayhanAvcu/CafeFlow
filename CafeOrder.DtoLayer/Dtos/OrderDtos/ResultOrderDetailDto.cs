namespace CafeOrder.DtoLayer.Dtos.OrderDtos;

/// <summary>
/// Siparişteki bir kalem. ProductImageUrl, ProductCategoryName, ProductCategorySlug sadece sepette dolu gelir
/// (kayıtlı siparişte ürün adı ve fiyatı zaten kopya olarak tutulur).
/// </summary>
public class ResultOrderDetailDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public string? ProductCategoryName { get; set; }
    public string? ProductCategorySlug { get; set; }
    public decimal BasePrice { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
    public List<ResultOrderDetailOptionDto> Options { get; set; } = new List<ResultOrderDetailOptionDto>();
}
