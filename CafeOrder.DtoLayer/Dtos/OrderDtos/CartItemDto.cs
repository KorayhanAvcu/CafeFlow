namespace CafeOrder.DtoLayer.Dtos.OrderDtos;

/// <summary>
/// Sepetteki bir satır: hangi ürün, hangi seçenekler, kaç tane. Session'da JSON olarak durur.
/// Fiyat burada tutulmaz; fiyatı her seferinde sunucu (OrderService) hesaplar.
/// </summary>
public class CartItemDto
{
    public int ProductId { get; set; }
    public List<int> OptionIds { get; set; } = new List<int>();
    public int Quantity { get; set; }
}
