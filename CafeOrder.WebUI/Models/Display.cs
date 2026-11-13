using System.Globalization;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.WebUI.Models;

/// <summary>Müşteri sayfalarında tekrar eden küçük yazı işleri (view'lar kullanır).</summary>
public static class Display
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>Fiyatı ₺1.234,50 biçiminde yazar.</summary>
    public static string Money(decimal amount)
    {
        return "₺" + amount.ToString("N2", Turkish);
    }

    /// <summary>Tarihi "13 Kasım, 10:30" biçiminde yazar.</summary>
    public static string DateTimeText(DateTime value)
    {
        return value.ToString("d MMMM, HH:mm", Turkish);
    }

    /// <summary>Ürün rozetinin CSS sınıfı.</summary>
    public static string BadgeClass(string? badge)
    {
        if (badge == "Çok Satan") return "cf-tag-best";
        if (badge == "Yeni") return "cf-tag-new";
        if (badge == "Popüler") return "cf-tag-popular";
        if (badge == "Şefin Seçimi") return "cf-tag-chef";
        return "";
    }

    /// <summary>
    /// Seçeneğin okunur hali: boyutta "Büyük" → "Büyük boy", ekstra shot'ta "Evet" → "Ekstra shot".
    /// Süt ve şurup adları veride zaten tam ("Yulaf Sütü", "Vanilya Şurubu"), olduğu gibi yazılır.
    /// </summary>
    public static string OptionText(string groupKey, string value)
    {
        if (groupKey == OptionGroups.Size) return value + " boy";
        if (groupKey == OptionGroups.ExtraShot) return "Ekstra shot";
        return value;
    }

    /// <summary>Sipariş türünün adı.</summary>
    public static string ChannelName(OrderChannel channel)
    {
        if (channel == OrderChannel.Delivery) return "Teslimat";
        if (channel == OrderChannel.Pickup) return "Gel-Al";
        return "Masada";
    }

    /// <summary>Sipariş türünün ikonu.</summary>
    public static string ChannelIcon(OrderChannel channel)
    {
        if (channel == OrderChannel.Delivery) return "bi-scooter";
        if (channel == OrderChannel.Pickup) return "bi-bag";
        return "bi-cup-hot";
    }

    /// <summary>Sipariş türünün kısa açıklaması.</summary>
    public static string ChannelText(OrderChannel channel)
    {
        if (channel == OrderChannel.Delivery) return "Kapınıza teslim edilir.";
        if (channel == OrderChannel.Pickup) return "Önceden sipariş verin, gelip alın.";
        return "Masanızdan sipariş verin.";
    }
}
