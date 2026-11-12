namespace CafeOrder.EntityLayer.Entities;

public class OrderDetailOption
{
    public int Id { get; set; }
    public int OrderDetailId { get; set; }
    public OrderDetail OrderDetail { get; set; } = null!;
    public int ProductOptionId { get; set; }
    public ProductOption ProductOption { get; set; } = null!;
    public string GroupKey { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public decimal PriceDelta { get; set; }
}