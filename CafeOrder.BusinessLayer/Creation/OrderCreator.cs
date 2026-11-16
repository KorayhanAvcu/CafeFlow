using AutoMapper;
using CafeOrder.BusinessLayer.States;
using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;

namespace CafeOrder.BusinessLayer.Creation;

/// <summary>
/// FACTORY METHOD deseni.
/// Her sipariş türü (teslimat, gel-al, masa) siparişi CreateForChannel metodunda kendisi üretir.
/// Bütün türlerde ortak olan kısım (numara, durum, tarih, ilk geçmiş kaydı) Create metodunda bir kez yazılır.
/// Creator'lar DTO ile çalışır; Order'ı elle değil AutoMapper ile oluşturur. Bağımlılıkların hepsi DI'dan gelir.
/// </summary>
public abstract class OrderCreator
{
    private readonly TimeProvider _time;
    private readonly IOrderStatusRecorder _statusRecorder;

    protected OrderCreator(IMapper mapper, TimeProvider time, IOrderStatusRecorder statusRecorder)
    {
        Mapper = mapper;
        _time = time;
        _statusRecorder = statusRecorder;
    }

    /// <summary>Alt sınıflar DTO'yu Order'a bununla çevirir.</summary>
    protected IMapper Mapper { get; }

    /// <summary>Bu sınıfın kurduğu sipariş türü.</summary>
    public abstract OrderChannel Channel { get; }

    /// <summary>
    /// Siparişi kurar: önce kanala özgü kısmı alt sınıfa yaptırır (CreateForChannel), sonra bütün kanallarda ortak olan
    /// sipariş numarası, "Alındı" durumu, tarih ve ilk durum geçmişi kaydını ekler. Adımların sırası burada sabittir (Template Method).
    /// </summary>
    public Order Create(CreateOrderDto dto, Customer customer, Table? table)
    {
        // 1) Türe özgü kısmı alt sınıf üretir.
        Order order = CreateForChannel(dto, customer, table);

        // 2) Ortak kısım
        order.OrderNo = CreateOrderNo();
        order.Status = OrderStatus.Received;
        order.CreatedAt = _time.GetLocalNow().DateTime;

        // 3) İlk durum kaydı: "Alındı"
        _statusRecorder.Record(order, null, OrderStatus.Received, order.CreatedAt);

        return order;
    }

    /// <summary>FACTORY METHOD: alt sınıf kendi kanalının siparişini üretir.</summary>
    protected abstract Order CreateForChannel(CreateOrderDto dto, Customer customer, Table? table);

    /// <summary>CF-1A2B3C4D gibi rastgele bir sipariş numarası üretir.</summary>
    private static string CreateOrderNo()
    {
        string guid = Guid.NewGuid().ToString("N");   // 32 karakter, tiresiz
        return "CF-" + guid.Substring(0, 8).ToUpper();
    }
}
