using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.DtoLayer.Dtos.OrderDtos;

/// <summary>
/// Sepet sayfasındaki sipariş formu. Ürünler bu DTO'da değil, Session'daki sepetten (CartItemDto) gelir.
/// Sipariş türü (Channel) de formdan değil Session'dan doldurulur.
/// </summary>
public class CreateOrderDto
{
    public OrderChannel Channel { get; set; }
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? AddressNotes { get; set; }
    public DateTime? PickupTime { get; set; }
    public string? Note { get; set; }
}
