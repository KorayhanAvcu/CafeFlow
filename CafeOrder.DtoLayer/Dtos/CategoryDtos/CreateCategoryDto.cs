namespace CafeOrder.DtoLayer.Dtos.CategoryDtos;

/// <summary>Yeni kategori formundan gelen veri. Kod, slug ve tarih sunucuda oluşur.</summary>
public class CreateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
