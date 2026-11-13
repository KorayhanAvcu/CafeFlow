namespace CafeOrder.DtoLayer.Dtos.OrderDtos;

/// <summary>
/// Ürün kartındaki veya ürün detayındaki "Sepete Ekle" formundan gelen veri.
/// Boyut, süt ve şurup radyo düğmesi (tek değer), ekstra shot onay kutusu (liste) olarak gelir.
/// Fiyat yoktur; fiyatı her zaman sunucu hesaplar.
/// </summary>
public class AddToCartDto
{
    public int ProductId { get; set; }
    public List<int> OptionIds { get; set; } = new List<int>();
    public int? SizeId { get; set; }
    public int? MilkId { get; set; }
    public int? SyrupId { get; set; }
    public int Quantity { get; set; } = 1;
    public string? ReturnUrl { get; set; }
}
