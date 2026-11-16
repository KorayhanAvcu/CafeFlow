using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.States;

/// <summary>
/// STATE deseni: her sipariş durumu bir sınıftır ve hangi duruma geçebileceğini kendisi söyler.
/// Akış: Received → Preparing → Ready → Delivered. İptal sadece Received ve Preparing'den yapılabilir.
/// Durum sınıfları DI'a kayıtlıdır; IOrderStateProvider hepsini alır ve Status'a göre verir.
/// Yeni bir durum = yeni bir sınıf + BusinessServiceRegistration'a bir satır.
/// </summary>
public abstract class OrderState
{
    /// <summary>Bu sınıfın temsil ettiği durum.</summary>
    public abstract OrderStatus Status { get; }

    /// <summary>Bu durumdan geçilebilecek durumlar.</summary>
    public abstract List<OrderStatus> GetAllowedNext();

    /// <summary>Hedef duruma geçiş izinli mi?</summary>
    public bool CanMoveTo(OrderStatus target)
    {
        return GetAllowedNext().Contains(target);
    }
}

/// <summary>Alındı → Hazırlanıyor veya İptal</summary>
public class ReceivedState : OrderState
{
    public override OrderStatus Status => OrderStatus.Received;

    /// <summary>Alındı durumundan Hazırlanıyor veya İptal'e geçilebilir.</summary>
    public override List<OrderStatus> GetAllowedNext()
    {
        return new List<OrderStatus> { OrderStatus.Preparing, OrderStatus.Cancelled };
    }
}

/// <summary>Hazırlanıyor → Hazır veya İptal</summary>
public class PreparingState : OrderState
{
    public override OrderStatus Status => OrderStatus.Preparing;

    /// <summary>Hazırlanıyor durumundan Hazır veya İptal'e geçilebilir.</summary>
    public override List<OrderStatus> GetAllowedNext()
    {
        return new List<OrderStatus> { OrderStatus.Ready, OrderStatus.Cancelled };
    }
}

/// <summary>Hazır → Teslim Edildi</summary>
public class ReadyState : OrderState
{
    public override OrderStatus Status => OrderStatus.Ready;

    /// <summary>Hazır durumundan sadece Teslim Edildi'ye geçilebilir.</summary>
    public override List<OrderStatus> GetAllowedNext()
    {
        return new List<OrderStatus> { OrderStatus.Delivered };
    }
}

/// <summary>Teslim Edildi: son durum, geçiş yok.</summary>
public class DeliveredState : OrderState
{
    public override OrderStatus Status => OrderStatus.Delivered;

    /// <summary>Son durum: hiçbir yere geçilemez.</summary>
    public override List<OrderStatus> GetAllowedNext()
    {
        return new List<OrderStatus>();
    }
}

/// <summary>İptal: son durum, geçiş yok.</summary>
public class CancelledState : OrderState
{
    public override OrderStatus Status => OrderStatus.Cancelled;

    /// <summary>Son durum: hiçbir yere geçilemez.</summary>
    public override List<OrderStatus> GetAllowedNext()
    {
        return new List<OrderStatus>();
    }
}
