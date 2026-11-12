namespace CafeOrder.DtoLayer.Dtos.ProductDtos;

/// <summary>
/// Ürün listelerinde ve ürün detayında ekrana giden veri.
/// CategoryName, CategorySlug, CategoryIcon: AutoMapper bunları Category.Name, Category.Slug,
/// Category.Icon'dan kendisi doldurur (isim birleştirme / "flattening").
/// </summary>
public class ResultProductDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public string? CategoryIcon { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public string? ImageUrl { get; set; }
    public string? Badge { get; set; }
    public bool IsVegetarian { get; set; }
    public bool IsCustomizable { get; set; }
    public bool IsActive { get; set; }
}
