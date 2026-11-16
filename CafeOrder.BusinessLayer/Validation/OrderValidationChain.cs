using CafeOrder.EntityLayer.Entities;

namespace CafeOrder.BusinessLayer.Validation;

/// <summary>
/// CHAIN OF RESPONSIBILITY zincirini DI ile kurar.
/// Halkalar BusinessServiceRegistration'da OrderValidationHandler olarak kayıtlıdır; kayıt sırası = zincir sırası.
/// Yeni bir kural eklemek için yeni bir halka sınıfı yazıp bir kayıt satırı eklemek yeterlidir; OrderService değişmez.
/// </summary>
public class OrderValidationChain : IOrderValidationChain
{
    private readonly OrderValidationHandler? _first;

    public OrderValidationChain(IEnumerable<OrderValidationHandler> handlers)
    {
        // Halkaları sırayla birbirine bağla: 1 → 2 → 3 → 4
        OrderValidationHandler? previous = null;
        foreach (OrderValidationHandler handler in handlers)
        {
            if (previous == null)
            {
                _first = handler;
            }
            else
            {
                previous.SetNext(handler);
            }
            previous = handler;
        }
    }

    /// <summary>Siparişi zincirin ilk halkasına verir; bir halka hata bulursa BusinessException fırlar.</summary>
    public void Validate(Order order)
    {
        _first?.Handle(order);
    }
}

/// <summary>Sipariş doğrulama zinciri.</summary>
public interface IOrderValidationChain
{
    /// <summary>Bütün kontrolleri sırayla çalıştırır; sorun varsa BusinessException fırlatır.</summary>
    void Validate(Order order);
}
