using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.States;

/// <summary>Durum değerine karşılık gelen State nesnesini verir.</summary>
public interface IOrderStateProvider
{
    /// <summary>Verilen durumun State nesnesi.</summary>
    OrderState Get(OrderStatus status);
}

/// <summary>
/// DI'a kayıtlı bütün OrderState sınıflarını alır ve Status'a göre bir sözlüğe koyar.
/// Strategy ve Factory Method ile aynı teknik: servis hangi durum sınıflarının olduğunu bilmez, new'lemez.
/// </summary>
public class OrderStateProvider : IOrderStateProvider
{
    private readonly IReadOnlyDictionary<OrderStatus, OrderState> _states;

    public OrderStateProvider(IEnumerable<OrderState> states)
    {
        // Aynı durum iki kez kaydedilirse burada hata verir (yanlış kaydı erken yakalar).
        _states = states.ToDictionary(s => s.Status);
    }

    /// <summary>Durumun State nesnesini verir; kaydı yoksa hangi durumun eksik olduğunu söyleyen bir hata fırlatır.</summary>
    public OrderState Get(OrderStatus status)
    {
        if (_states.TryGetValue(status, out OrderState? state))
        {
            return state;
        }
        throw new InvalidOperationException(status + " durumu için OrderState kaydı yok.");
    }
}
