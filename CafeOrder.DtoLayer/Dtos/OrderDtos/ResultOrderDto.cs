using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.DtoLayer.Dtos.OrderDtos;

/// <summary>
/// Sipariş (veya hesaplanmış sepet) ekrana böyle gider.
/// CustomerFullName, CustomerPhone, TableNumber, TableArea: AutoMapper bunları
/// Customer.FullName, Customer.Phone, Table.Number, Table.Area'dan kendisi doldurur.
/// </summary>
public class ResultOrderDto
{
    public int Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public OrderChannel Channel { get; set; }
    public OrderStatus Status { get; set; }

    public string? CustomerFullName { get; set; }
    public string? CustomerPhone { get; set; }
    public int? TableNumber { get; set; }
    public string? TableArea { get; set; }

    public string? Address { get; set; }
    public string? AddressNotes { get; set; }
    public DateTime? PickupTime { get; set; }
    public string? Note { get; set; }

    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<ResultOrderDetailDto> Details { get; set; } = new List<ResultOrderDetailDto>();
    public List<ResultOrderStatusHistoryDto> StatusHistories { get; set; } = new List<ResultOrderStatusHistoryDto>();

    /// <summary>
    /// Bu durumdan geçilebilecek durumlar (STATE deseni). OrderService.GetByIdAsync doldurur;
    /// admin sipariş detayı sadece bunların düğmesini gösterir. Boşsa sipariş son durumundadır.
    /// </summary>
    public List<OrderStatus> AllowedNextStatuses { get; set; } = new List<OrderStatus>();
}
