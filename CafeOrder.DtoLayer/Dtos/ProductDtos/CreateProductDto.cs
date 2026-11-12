namespace CafeOrder.DtoLayer.Dtos.ProductDtos;

/// <summary>Yeni ürün formundan gelen veri.</summary>
public class CreateProductDto
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public string? ImageUrl { get; set; }
    public string? Badge { get; set; }
    public bool IsVegetarian { get; set; }
    public bool IsCustomizable { get; set; }
    public bool IsActive { get; set; } = true;
}
