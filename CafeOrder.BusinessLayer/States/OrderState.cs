using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.States;

/// <summary>
/// STATE deseni: her sipariş durumu bir sınıftır ve hangi duruma geçebileceğini kendisi söyler.
/// Akış: Received → Preparing → Ready → Delivered. İptal sadece Received ve Preparing'den yapılabilir.
/// </summary>
public abstract class OrderState
{
    /// <summary>Bu durumdan geçilebilecek durumlar.</summary>
    public abstract List<OrderStatus> GetAllowedNext();

    /// <summary>Hedef duruma geçiş izinli mi?</summary>
    public bool CanMoveTo(OrderStatus target)
    {
        return GetAllowedNext().Contains(target);
    }

    /// <summary>Durum değerine karşılık gelen sınıfı verir.</summary>
    public static OrderState For(OrderStatus status)
    {
        if (status == OrderStatus.Received)
        {
            return new ReceivedState();
        }
        if (status == OrderStatus.Preparing)
        {
            return new PreparingState();
        }
        if (status == OrderStatus.Ready)
        {
            return new ReadyState();
        }
        if (status == OrderStatus.Delivered)
        {
            return new DeliveredState();
        }
        return new CancelledState();
    }
}

/// <summary>Alındı → Hazırlanıyor veya İptal</summary>
public class ReceivedState : OrderState
{
    /// <summary>Alındı durumundan Hazırlanıyor veya İptal'e geçilebilir.</summary>
    public override List<OrderStatus> GetAllowedNext()
    {
        return new List<OrderStatus> { OrderStatus.Preparing, OrderStatus.Cancelled };
    }
}

/// <summary>Hazırlanıyor → Hazır veya İptal</summary>
public class PreparingState : OrderState
{
    /// <summary>Hazırlanıyor durumundan Hazır veya İptal'e geçilebilir.</summary>
    public override List<OrderStatus> GetAllowedNext()
    {
        return new List<OrderStatus> { OrderStatus.Ready, OrderStatus.Cancelled };
    }
}

/// <summary>Hazır → Teslim Edildi</summary>
public class ReadyState : OrderState
{
    /// <summary>Hazır durumundan sadece Teslim Edildi'ye geçilebilir (artık iptal edilemez).</summary>
    public override List<OrderStatus> GetAllowedNext()
    {
        return new List<OrderStatus> { OrderStatus.Delivered };
    }
}

/// <summary>Teslim Edildi: son durum, başka yere geçilemez.</summary>
public class DeliveredState : OrderState
{
    /// <summary>Teslim Edildi son durumdur; boş liste döner, hiçbir yere geçilemez.</summary>
    public override List<OrderStatus> GetAllowedNext()
    {
        return new List<OrderStatus>();
    }
}

/// <summary>İptal: son durum, başka yere geçilemez.</summary>
public class CancelledState : OrderState
{
    /// <summary>İptal son durumdur; boş liste döner, hiçbir yere geçilemez.</summary>
    public override List<OrderStatus> GetAllowedNext()
    {
        return new List<OrderStatus>();
    }
}
