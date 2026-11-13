using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Services.Cart;

/// <summary>
/// Sepet işlemleri. Sepetin kendisi (List&lt;CartItemDto&gt;) Session'da durur; Session web katmanına ait olduğu için
/// controller sepeti Session'dan okuyup bu servise verir, servisin değiştirdiği listeyi geri yazar.
/// </summary>
public interface ICartService
{
    /// <summary>Ürünü sepete ekler (aynı ürün + aynı seçenekler varsa adedi artırır). Eklenen ürünün adını döndürür; ürün satışta değilse BusinessException.</summary>
    Task<string> AddItemAsync(List<CartItemDto> cart, AddToCartDto dto);

    /// <summary>Sepetteki index'inci satırın adedini değiştirir; adet 0 veya altıysa satırı siler.</summary>
    void UpdateQuantity(List<CartItemDto> cart, int index, int quantity);

    /// <summary>Sepet sayfasının bütün verisini hazırlar: fiyatlandırılmış sepet, temizlenmiş satırlar, masa, form, hata.</summary>
    Task<ResultCartDto> GetCartAsync(List<CartItemDto> cart, OrderChannel? channel, string? qrToken, CreateOrderDto? form, string? error);

    /// <summary>Siparişi verir: sipariş türünü Session'daki seçimden alır (seçilmediyse Teslimat) ve OrderService.PlaceOrderAsync'i çağırır.</summary>
    Task<ResultOrderDto> CheckoutAsync(List<CartItemDto> cart, OrderChannel? channel, string? qrToken, CreateOrderDto form);
}
