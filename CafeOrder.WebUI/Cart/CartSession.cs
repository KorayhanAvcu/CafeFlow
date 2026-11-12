using System.Text.Json;
using CafeOrder.DtoLayer.Dtos.OrderDtos;

namespace CafeOrder.WebUI.Cart;

/// <summary>
/// Sepet, tarayıcıya özel Session'da JSON yazısı olarak durur. Sipariş verilince veritabanına yazılır.
/// Sipariş türü ve masa kodu da Session'da birer yazı olarak tutulur.
/// Metotlar "extension method": HttpContext.Session.GetCart() diye çağrılır.
/// </summary>
public static class CartSession
{
    private const string CartKey = "Cart";
    public const string ChannelKey = "Channel";
    public const string TableKey = "QrToken";

    /// <summary>Sepeti okur; yoksa boş liste döner.</summary>
    public static List<CartItemDto> GetCart(this ISession session)
    {
        string? json = session.GetString(CartKey);
        if (json == null)
        {
            return new List<CartItemDto>();
        }
        return JsonSerializer.Deserialize<List<CartItemDto>>(json) ?? new List<CartItemDto>();
    }

    /// <summary>Sepeti Session'a yazar.</summary>
    public static void SaveCart(this ISession session, List<CartItemDto> cart)
    {
        string json = JsonSerializer.Serialize(cart);
        session.SetString(CartKey, json);
    }

    /// <summary>Sepetteki toplam ürün adedi (navbar rozeti).</summary>
    public static int CartCount(this ISession session)
    {
        int total = 0;
        foreach (CartItemDto item in session.GetCart())
        {
            total += item.Quantity;
        }
        return total;
    }
}
