namespace CafeOrder.DtoLayer.Dtos.OrderDtos;

/// <summary>Panel (dashboard) sayfasındaki "En çok satanlar" tablosunun bir satırı.</summary>
public class ResultBestSellerDto
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
}
