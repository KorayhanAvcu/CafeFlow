using System.Globalization;
using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.WebUI.Models;

/// <summary>Admin ekranlarında tekrar eden yazı, ikon ve renk seçimleri (view'lar kullanır).</summary>
public static class AdminDisplay
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>Fiyatı ₺1.250,50 biçiminde yazar.</summary>
    public static string Money(decimal amount)
    {
        return "₺" + amount.ToString("N2", Turkish);
    }

    /// <summary>Tarihi "12 Kas 2026, 20:50" biçiminde yazar.</summary>
    public static string Date(DateTime value)
    {
        return value.ToString("d MMM yyyy, HH:mm", Turkish);
    }

    /// <summary>Tarihi "13 Kasım 2026 Cuma" biçiminde yazar (dashboard başlığı).</summary>
    public static string LongDate(DateTime value)
    {
        return value.ToString("d MMMM yyyy dddd", Turkish);
    }

    /// <summary>Durumun ekrandaki adı.</summary>
    public static string StatusLabel(OrderStatus status)
    {
        return status.ToText();
    }

    /// <summary>Bildirim hedefinin ekrandaki adı.</summary>
    public static string TargetLabel(NotificationTarget target)
    {
        return target.ToText();
    }

    /// <summary>Seçenek grubunun ekrandaki adı: "Size" → "Boyut".</summary>
    public static string OptionGroupLabel(string groupKey)
    {
        if (groupKey == OptionGroups.Size) return "Boyut";
        if (groupKey == OptionGroups.Milk) return "Süt";
        if (groupKey == OptionGroups.ExtraShot) return "Ekstra shot";
        if (groupKey == OptionGroups.Syrup) return "Şurup";
        if (groupKey == OptionGroups.Cream) return "Krema";
        return groupKey;
    }

    /// <summary>Durum rozetinin CSS sınıfı (rengi).</summary>
    public static string StatusBadge(OrderStatus status)
    {
        if (status == OrderStatus.Received) return "adm-badge-info";
        if (status == OrderStatus.Preparing) return "adm-badge-warning";
        if (status == OrderStatus.Ready) return "adm-badge-success";
        if (status == OrderStatus.Delivered) return "adm-badge-neutral";
        return "adm-badge-inactive";
    }

    /// <summary>Sipariş detayındaki durum düğmesinin yazısı.</summary>
    public static string ActionLabel(OrderStatus target)
    {
        if (target == OrderStatus.Preparing) return "Hazırlamaya başla";
        if (target == OrderStatus.Ready) return "Hazır olarak işaretle";
        if (target == OrderStatus.Delivered) return "Teslim edildi olarak işaretle";
        return "Siparişi iptal et";
    }

    /// <summary>Durum düğmesinin ikonu.</summary>
    public static string ActionIcon(OrderStatus target)
    {
        if (target == OrderStatus.Preparing) return "bi-fire";
        if (target == OrderStatus.Ready) return "bi-bell";
        if (target == OrderStatus.Delivered) return "bi-check2-all";
        return "bi-x-circle";
    }

    /// <summary>Durum düğmesinin rengi: iptal kırmızı, diğerleri ana renk.</summary>
    public static string ActionButtonClass(OrderStatus target)
    {
        if (target == OrderStatus.Cancelled) return "adm-btn-danger";
        return "adm-btn-primary";
    }

    /// <summary>Sipariş türünün adı.</summary>
    public static string ChannelLabel(OrderChannel channel)
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
        return "bi-qr-code";
    }

    /// <summary>Listede "kimin siparişi" sütunu: masa siparişinde masa, diğerlerinde müşteri adı.</summary>
    public static string Who(ResultOrderDto order)
    {
        if (order.Channel == OrderChannel.Table)
        {
            if (order.TableNumber == null)
            {
                return "Masa";
            }
            return "Masa " + order.TableNumber + " · " + order.TableArea;
        }
        if (order.CustomerFullName == null)
        {
            return "—";
        }
        return order.CustomerFullName;
    }

    /// <summary>Ad soyadın baş harfleri: "Selin Aydın" → "SA".</summary>
    public static string Initials(string fullName)
    {
        string[] parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return "?";
        }
        string first = parts[0].Substring(0, 1);
        if (parts.Length == 1)
        {
            return first.ToUpper(Turkish);
        }
        string last = parts[parts.Length - 1].Substring(0, 1);
        return (first + last).ToUpper(Turkish);
    }
}
