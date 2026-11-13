using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Creation;

/// <summary>
/// FACTORY METHOD deseni.
/// Her sipariş türü (teslimat, gel-al, masa) kendi alanlarını CreateForChannel metodunda doldurur.
/// Bütün türlerde ortak olan kısım (numara, durum, tarih, ilk geçmiş kaydı) Create metodunda bir kez yazılır.
/// </summary>
public abstract class OrderCreator
{
    /// <summary>Siparişi kurar: önce kanala özgü kısmı alt sınıfa yaptırır (CreateForChannel), sonra bütün kanallarda ortak olan sipariş numarası, "Alındı" durumu, tarih, not ve ilk durum geçmişi kaydını ekler. Adımların sırası burada sabittir (Template Method).</summary>
    public Order Create(Order form, Table? table)
    {
        // 1) Türe özgü kısmı alt sınıf doldurur.
        Order order = CreateForChannel(form, table);

        // 2) Ortak kısım
        order.OrderNo = CreateOrderNo();
        order.Status = OrderStatus.Received;
        order.Note = form.Note;
        order.CreatedAt = DateTime.Now;

        // 3) İlk durum kaydı: "Alındı"
        OrderStatusHistory history = new OrderStatusHistory();
        history.ToStatus = OrderStatus.Received;
        history.ChangedAt = order.CreatedAt;
        order.StatusHistories.Add(history);

        return order;
    }

    /// <summary>Alt sınıfların yazmak zorunda olduğu metot.</summary>
    protected abstract Order CreateForChannel(Order form, Table? table);

    /// <summary>CF-1A2B3C4D gibi rastgele bir sipariş numarası üretir.</summary>
    private string CreateOrderNo()
    {
        string guid = Guid.NewGuid().ToString("N");   // 32 karakter, tiresiz
        return "CF-" + guid.Substring(0, 8).ToUpper();
    }
}
