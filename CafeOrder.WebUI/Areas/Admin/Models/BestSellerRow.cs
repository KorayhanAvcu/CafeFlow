namespace CafeOrder.WebUI.Areas.Admin.Models;

/// <summary>Dashboard'daki "Best sellers" tablosunun bir satırı.</summary>
public class BestSellerRow
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
}
