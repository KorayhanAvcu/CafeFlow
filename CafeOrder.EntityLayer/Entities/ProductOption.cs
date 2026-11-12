namespace CafeOrder.EntityLayer.Entities;

public class ProductOption
{
    public int Id { get; set; }
    public string GroupKey { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public decimal PriceDelta { get; set; }
    public bool IsDefault { get; set; }
}