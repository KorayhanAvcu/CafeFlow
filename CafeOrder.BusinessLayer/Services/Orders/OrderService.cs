using CafeOrder.BusinessLayer.Creation;
using CafeOrder.BusinessLayer.Events;
using CafeOrder.BusinessLayer.Exceptions;
using CafeOrder.BusinessLayer.States;
using CafeOrder.BusinessLayer.Validation;
using CafeOrder.DataAccessLayer.Repositories.Customers;
using CafeOrder.DataAccessLayer.Repositories.Orders;
using CafeOrder.DataAccessLayer.Repositories.Tables;
using CafeOrder.DataAccessLayer.UnitOfWork;
using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;
using MapsterMapper;

namespace CafeOrder.BusinessLayer.Services.Orders;

/// <summary>
/// Sipariş verme ve durum değiştirme. Desenler burada sırayla çalışır:
/// Sipariş verilirken: Factory Method → Decorator + Strategy → Chain of Responsibility → Observer → kayıt
/// Durum değişirken:  State → Observer → kayıt
/// Desenlerin hepsi DI'dan gelir; bu sınıf hiçbir servisi new'lemez.
/// Sipariş ve bildirimler tek SaveChanges ile (Unit of Work) yazılır: ya hepsi ya hiçbiri.
/// </summary>
public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ITableRepository _tableRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IReadOnlyDictionary<OrderChannel, OrderCreator> _creators;
    private readonly IOrderPricingService _pricing;
    private readonly IOrderValidationChain _validation;
    private readonly IOrderEventPublisher _events;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;
    private readonly IMapper _mapper;

    public OrderService(
        IOrderRepository orderRepository,
        ITableRepository tableRepository,
        ICustomerRepository customerRepository,
        IEnumerable<OrderCreator> creators,
        IOrderPricingService pricing,
        IOrderValidationChain validation,
        IOrderEventPublisher events,
        IUnitOfWork unitOfWork,
        TimeProvider time,
        IMapper mapper)
    {
        _orderRepository = orderRepository;
        _tableRepository = tableRepository;
        _customerRepository = customerRepository;
        // Aynı kanal için iki creator kaydedilirse burada hata verir (yanlış kaydı erken yakalar).
        _creators = creators.ToDictionary(c => c.Channel);
        _pricing = pricing;
        _validation = validation;
        _events = events;
        _unitOfWork = unitOfWork;
        _time = time;
        _mapper = mapper;
    }

    // ---------------- Sipariş verme ----------------

    /// <summary>Sepet sayfasındaki "Siparişi Ver" düğmesi buraya gelir.</summary>
    public async Task<ResultOrderDto> PlaceOrderAsync(CreateOrderDto dto, List<CartItemDto> items, string? qrToken)
    {
        // DTO → Entity: adres, not, alma saati, sipariş türü kopyalanır.
        Order form = _mapper.Map<Order>(dto);
        form.Customer = await FindOrCreateCustomerAsync(dto.FullName, dto.Phone);

        // Masa siparişinde masa QR koduyla bulunur; müşteri masa numarası yazamaz.
        Table? table = null;
        if (form.Channel == OrderChannel.Table && !string.IsNullOrWhiteSpace(qrToken))
        {
            string token = qrToken.ToUpper();
            table = await _tableRepository.GetFirstAsync(t => t.QrToken == token && t.IsActive);
        }

        // 1) FACTORY METHOD: türe uygun sipariş nesnesi kurulur.
        Order order = GetCreator(form.Channel).Create(form, table);

        // 2) DECORATOR + STRATEGY: fiyatlar ve ücret hesaplanır.
        await _pricing.ApplyPricesAsync(order, items);

        // 3) CHAIN OF RESPONSIBILITY: kontroller sırayla; biri hata bulursa sipariş kaydedilmez.
        _validation.Validate(order);

        // 4) Sipariş "eklenecek" işaretlenir; OBSERVER: mutfak, kasa ve admin bildirimleri eklenir.
        await _orderRepository.AddAsync(order);
        await _events.OrderPlacedAsync(order);

        // 5) UNIT OF WORK: sipariş, kalemler, ilk durum kaydı, yeni müşteri ve bildirimler tek transaction'da yazılır.
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ResultOrderDto>(order);
    }

    // ---------------- Durum değiştirme ----------------

    /// <summary>Admin sipariş detayındaki durum düğmeleri buraya gelir.</summary>
    public async Task ChangeStatusAsync(int id, OrderStatus target, string? note)
    {
        var order = await _orderRepository.GetWithDetailsAsync(id);
        if (order == null)
        {
            throw new BusinessException("Sipariş bulunamadı.");
        }

        // STATE: şu anki durum, hedef duruma geçişe izin veriyor mu?
        OrderState state = OrderState.For(order.Status);
        if (!state.CanMoveTo(target))
        {
            throw new BusinessException("Sipariş \"" + order.Status.ToText() + "\" durumundan \"" + target.ToText() + "\" durumuna geçemez.");
        }

        OrderStatus previous = order.Status;
        order.Status = target;

        // Geçmişe yeni bir satır ekle.
        OrderStatusHistory history = new OrderStatusHistory();
        history.FromStatus = previous;
        history.ToStatus = target;
        history.ChangedAt = _time.GetLocalNow().DateTime;
        if (!string.IsNullOrWhiteSpace(note))
        {
            history.Note = note.Trim();
        }
        order.StatusHistories.Add(history);

        // OBSERVER: ilgililere haber ver.
        await _events.StatusChangedAsync(order, previous);

        // Sipariş veritabanından takip edilerek (tracking) okundu; EF değişen durumu ve yeni satırları kendisi bulur.
        await _unitOfWork.SaveChangesAsync();
    }

    // ---------------- Yardımcılar ----------------

    /// <summary>FACTORY METHOD: kanala kayıtlı creator'ı verir.</summary>
    private OrderCreator GetCreator(OrderChannel channel)
    {
        if (_creators.TryGetValue(channel, out OrderCreator? creator))
        {
            return creator;
        }
        throw new InvalidOperationException(channel + " kanalı için OrderCreator kaydı yok.");
    }

    /// <summary>Aynı telefonla daha önce sipariş veren müşteri tekrar oluşturulmaz; yoksa yeni müşteri nesnesi (kayıtta eklenir).</summary>
    private async Task<Customer> FindOrCreateCustomerAsync(string? fullName, string? phone)
    {
        Customer customer = new Customer();
        customer.FullName = (fullName ?? "").Trim();
        customer.Phone = (phone ?? "").Trim();
        if (customer.Phone != "")
        {
            string p = customer.Phone;
            var existing = await _customerRepository.GetFirstAsync(c => c.Phone == p);
            if (existing != null)
            {
                return existing;
            }
        }
        return customer;
    }
}
