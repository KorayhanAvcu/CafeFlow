using System.Text.Json;
using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.EntityLayer.Enums;

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

    /// <summary>Sepeti boşaltır (sipariş verildikten sonra veya "Sepeti boşalt" düğmesiyle).</summary>
    public static void ClearCart(this ISession session)
    {
        session.SaveCart(new List<CartItemDto>());
    }

    /// <summary>Seçilen sipariş türünü okur; seçilmemişse veya geçersizse null.</summary>
    public static OrderChannel? GetChannel(this ISession session)
    {
        if (Enum.TryParse(session.GetString(ChannelKey), out OrderChannel channel))
        {
            return channel;
        }
        return null;
    }

    /// <summary>Seçilen sipariş türünü yazar.</summary>
    public static void SetChannel(this ISession session, OrderChannel channel)
    {
        session.SetString(ChannelKey, channel.ToString());
    }

    /// <summary>Okutulan masanın QR kodunu okur; yoksa null.</summary>
    public static string? GetQrToken(this ISession session)
    {
        return session.GetString(TableKey);
    }

    /// <summary>QR ile okutulan masayı kaydeder: QR kodu yazılır ve sipariş türü Masa yapılır.</summary>
    public static void SetTable(this ISession session, string qrToken)
    {
        session.SetString(TableKey, qrToken);
        session.SetChannel(OrderChannel.Table);
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
