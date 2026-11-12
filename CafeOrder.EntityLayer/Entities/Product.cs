namespace CafeOrder.EntityLayer.Entities;

/// <summary>Menüdeki ürün (Products tablosu).</summary>
public class Product
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }      // seçeneksiz fiyat
    public string? ImageUrl { get; set; }
    public string? Badge { get; set; }          // Best Seller, New, Popular, Chef's Choice
    public bool IsVegetarian { get; set; }
    public bool IsCustomizable { get; set; }    // boyut, süt, shot, şurup seçilebilir mi
    public bool IsActive { get; set; } = true;
}
