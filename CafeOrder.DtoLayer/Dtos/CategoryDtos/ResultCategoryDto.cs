namespace CafeOrder.DtoLayer.Dtos.CategoryDtos;

/// <summary>Kategori listelerinde ekrana giden veri.</summary>
public class ResultCategoryDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Kategorideki ürün sayısı (GeneralMapping'de Products.Count'tan doldurulur).</summary>
    public int ProductCount { get; set; }
}
