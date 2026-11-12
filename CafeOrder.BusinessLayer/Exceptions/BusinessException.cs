namespace CafeOrder.BusinessLayer.Exceptions;

/// <summary>Bir iş kuralı bozulduğunda fırlatılır. Controller yakalar ve mesajı ekranda gösterir.</summary>
public class BusinessException : Exception
{
    public BusinessException(string message) : base(message)
    {
    }
}
