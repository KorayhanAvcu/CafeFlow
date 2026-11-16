using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.States;

/// <summary>Siparişin durum geçmişine satır ekler.</summary>
public interface IOrderStatusRecorder
{
    /// <summary>"from → to" geçişini (isteğe bağlı notla) siparişin geçmişine ekler. from null ise ilk kayıttır.</summary>
    void Record(Order order, OrderStatus? from, OrderStatus to, DateTime at, string? note = null);
}

/// <summary>
/// Durum geçmişi iki yerde yazılıyor: sipariş oluşurken ("Alındı") ve admin durumu değiştirirken.
/// İkisi de aynı kuralı (notu kırp, boşsa null) bu tek sınıftan kullanıyor.
/// </summary>
public class OrderStatusRecorder : IOrderStatusRecorder
{
    /// <summary>Yeni satır sipariş izlendiği için SaveChanges'te otomatik INSERT edilir.</summary>
    public void Record(Order order, OrderStatus? from, OrderStatus to, DateTime at, string? note = null)
    {
        order.StatusHistories.Add(new OrderStatusHistory
        {
            FromStatus = from,
            ToStatus = to,
            ChangedAt = at,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        });
    }
}
