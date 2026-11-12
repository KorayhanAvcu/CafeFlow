using System.Globalization;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.WebUI.Models;

/// <summary>Müşteri sayfalarında tekrar eden küçük yazı işleri (view'lar kullanır).</summary>
public static class Display
{
    /// <summary>Fiyatı ₺1,234.50 biçiminde yazar.</summary>
    public static string Money(decimal amount)
    {
        return "₺" + amount.ToString("N2", CultureInfo.GetCultureInfo("en-US"));
    }

    /// <summary>Ürün rozetinin CSS sınıfı.</summary>
    public static string BadgeClass(string? badge)
    {
        if (badge == "Best Seller") return "cf-tag-best";
        if (badge == "New") return "cf-tag-new";
        if (badge == "Popular") return "cf-tag-popular";
        if (badge == "Chef's Choice") return "cf-tag-chef";
        return "";
    }

    /// <summary>Seçeneğin okunur hali: "Oat" → "Oat milk".</summary>
    public static string OptionText(string groupKey, string value)
    {
        if (groupKey == OptionGroups.Size) return value + " size";
        if (groupKey == OptionGroups.Milk) return value + " milk";
        if (groupKey == OptionGroups.ExtraShot) return "Extra shot";
        return value + " syrup";
    }

    /// <summary>Sipariş türünün adı.</summary>
    public static string ChannelName(OrderChannel channel)
    {
        if (channel == OrderChannel.Delivery) return "Delivery";
        if (channel == OrderChannel.Pickup) return "Pickup";
        return "Dine-In";
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
        if (channel == OrderChannel.Delivery) return "Delivered to your door.";
        if (channel == OrderChannel.Pickup) return "Order ahead, pick it up.";
        return "Order from your table.";
    }
}
