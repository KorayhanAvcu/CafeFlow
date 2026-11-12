namespace CafeOrder.EntityLayer.Entities;

public class Table
{
    public int Id { get; set; }
    public int Number { get; set; }
    public string Area { get; set; } = string.Empty;
    public string QrToken { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<Order> Orders { get; set; } = new();
}