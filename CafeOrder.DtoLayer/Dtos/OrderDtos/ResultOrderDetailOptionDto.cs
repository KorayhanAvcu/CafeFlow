namespace CafeOrder.DtoLayer.Dtos.OrderDtos;

/// <summary>Kalemde seçilen bir seçenek (örneğin Size: Large, +30 ₺).</summary>
public class ResultOrderDetailOptionDto
{
    public int ProductOptionId { get; set; }
    public string GroupKey { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public decimal PriceDelta { get; set; }
}
