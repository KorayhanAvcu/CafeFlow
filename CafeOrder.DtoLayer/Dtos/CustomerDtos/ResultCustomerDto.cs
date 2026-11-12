namespace CafeOrder.DtoLayer.Dtos.CustomerDtos;

/// <summary>Admin müşteri listesindeki bir satır.</summary>
public class ResultCustomerDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int OrderCount { get; set; }

    /// <summary>İptal edilmemiş siparişlerin toplamı.</summary>
    public decimal TotalSpent { get; set; }

    /// <summary>Son sipariş tarihi; hiç sipariş yoksa null.</summary>
    public DateTime? LastOrderDate { get; set; }
}
