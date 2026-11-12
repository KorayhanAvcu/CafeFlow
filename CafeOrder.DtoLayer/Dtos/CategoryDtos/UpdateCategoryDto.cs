namespace CafeOrder.DtoLayer.Dtos.CategoryDtos;

/// <summary>
/// Kategori düzenleme formunun verisi. Form sayfası bu DTO ile açılır;
/// Id = 0 ise kayıt yeni demektir (controller CreateCategoryDto'ya çevirir).
/// </summary>
public class UpdateCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
