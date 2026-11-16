namespace CafeOrder.DataAccessLayer.Repositories.Orders;

/// <summary>En çok satanlar sorgusunun bir satırı: ürün adı, satılan adet ve ciro.</summary>
public record ProductSales(string ProductName, int Quantity, decimal Revenue);
