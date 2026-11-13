namespace CafeOrder.EntityLayer.Enums;

/// <summary>
/// Enum'ların ekranda ve mesajlarda görünen Türkçe adları.
/// Veritabanında enum adı (Received, Delivery ...) saklanır; kullanıcı her zaman bu metinleri görür.
/// Hem BusinessLayer (hata ve bildirim mesajları) hem WebUI (sayfalar) kullandığı için en alttaki katmanda durur.
/// Kullanım: order.Status.ToText()  →  "Hazırlanıyor"
/// </summary>
public static class EnumTexts
{
    /// <summary>Sipariş durumunun Türkçe adı: Preparing → "Hazırlanıyor".</summary>
    public static string ToText(this OrderStatus status)
    {
        if (status == OrderStatus.Received) return "Alındı";
        if (status == OrderStatus.Preparing) return "Hazırlanıyor";
        if (status == OrderStatus.Ready) return "Hazır";
        if (status == OrderStatus.Delivered) return "Teslim Edildi";
        return "İptal Edildi";
    }

    /// <summary>Sipariş kanalının Türkçe adı: Pickup → "Gel-Al".</summary>
    public static string ToText(this OrderChannel channel)
    {
        if (channel == OrderChannel.Delivery) return "Teslimat";
        if (channel == OrderChannel.Pickup) return "Gel-Al";
        return "Masa";
    }

    /// <summary>Bildirim hedefinin Türkçe adı: Kitchen → "Mutfak".</summary>
    public static string ToText(this NotificationTarget target)
    {
        if (target == NotificationTarget.Kitchen) return "Mutfak";
        if (target == NotificationTarget.Cashier) return "Kasa";
        return "Yönetici";
    }
}
