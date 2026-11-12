using CafeOrder.EntityLayer.Entities;

namespace CafeOrder.BusinessLayer.Validation;

/// <summary>
/// CHAIN OF RESPONSIBILITY deseni: her kontrol zincirin bir halkasıdır.
/// Halka kendi kontrolünü yapar; sorun varsa BusinessException fırlatır ve zincir durur,
/// sorun yoksa sipariş sıradaki halkaya geçer.
/// </summary>
public abstract class OrderValidationHandler
{
    private OrderValidationHandler? _next;

    /// <summary>Sıradaki halkayı bağlar. Sıradakini döndürür ki zincir yan yana yazılabilsin.</summary>
    public OrderValidationHandler SetNext(OrderValidationHandler next)
    {
        _next = next;
        return next;
    }

    /// <summary>Önce kendi kontrolünü yapar, sonra sıradaki halkaya geçer.</summary>
    public void Handle(Order order)
    {
        Check(order);

        if (_next != null)
        {
            _next.Handle(order);
        }
    }

    /// <summary>Her halkanın kendi kontrolü.</summary>
    protected abstract void Check(Order order);
}
