using MapsterMapper;
using CafeOrder.BusinessLayer.Creation;
using CafeOrder.BusinessLayer.Events;
using CafeOrder.BusinessLayer.Exceptions;
using CafeOrder.BusinessLayer.Pricing;
using CafeOrder.BusinessLayer.Pricing.Fees;
using CafeOrder.BusinessLayer.States;
using CafeOrder.BusinessLayer.Validation;
using CafeOrder.DataAccessLayer.Repositories.Customers;
using CafeOrder.DataAccessLayer.Repositories.Orders;
using CafeOrder.DataAccessLayer.Repositories.ProductOptions;
using CafeOrder.DataAccessLayer.Repositories.Products;
using CafeOrder.DataAccessLayer.Repositories.Tables;
using CafeOrder.DtoLayer.Dtos.OrderDtos;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;
using Microsoft.EntityFrameworkCore;

namespace CafeOrder.BusinessLayer.Services.Orders;

/// <summary>
/// Sipariş işleri. Desenler burada sırayla çalışır:
/// Sipariş verilirken: Factory Method → Decorator → Strategy → Chain of Responsibility → kayıt → Observer
/// Durum değişirken:  State → kayıt → Observer
/// Desenler Entity (Order) üzerinde çalışır; dışarıya sonuç ResultOrderDto olarak verilir.
/// </summary>
public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IProductOptionRepository _productOptionRepository;
    private readonly ITableRepository _tableRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly PricingOptions _pricing;
    private readonly OrderEventPublisher _events;
    private readonly IMapper _mapper;

    public OrderService(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IProductOptionRepository productOptionRepository,
        ITableRepository tableRepository,
        ICustomerRepository customerRepository,
        PricingOptions pricing,
        OrderEventPublisher events,
        IMapper mapper)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _productOptionRepository = productOptionRepository;
        _tableRepository = tableRepository;
        _customerRepository = customerRepository;
        _pricing = pricing;
        _events = events;
        _mapper = mapper;
    }

    // ---------------- Okuma ----------------

    /// <summary>Admin listesi ve dashboard: bütün siparişler, yeniden eskiye.</summary>
    public async Task<List<ResultOrderDto>> GetAllAsync()
    {
        var orders = await _orderRepository.GetQueryable()
            .Include(o => o.Customer)
            .Include(o => o.Table)
            .Include(o => o.Details)
            .OrderByDescending(o => o.Id)
            .ToListAsync();

        return _mapper.Map<List<ResultOrderDto>>(orders);
    }

    /// <summary>Siparişi Id ile, kalemleri/seçenekleri/geçmişiyle birlikte getirir ve DTO'ya çevirir. Yoksa null.</summary>
    public async Task<ResultOrderDto?> GetByIdAsync(int id)
    {
        var order = await OrdersWithDetails().FirstOrDefaultAsync(o => o.Id == id);
        if (order == null)
        {
            return null;
        }
        ResultOrderDto dto = _mapper.Map<ResultOrderDto>(order);

        // STATE: bu durumdan hangi durumlara geçilebilir? Admin sayfası sadece bunların düğmesini gösterir.
        dto.AllowedNextStatuses = OrderState.For(order.Status).GetAllowedNext();
        return dto;
    }

    /// <summary>
    /// Admin sipariş listesi. Önce her durumdaki sipariş sayısını hesaplar (üstteki kartlar, filtreden önce),
    /// sonra durum, kanal ve aramaya (sipariş no, müşteri adı, telefon) göre filtreler ve 10'arlı sayfalara böler.
    /// </summary>
    public async Task<ResultOrderListDto> GetAdminListAsync(OrderFilterDto filter)
    {
        const int pageSize = 10;
        var orders = await GetAllAsync();

        ResultOrderListDto result = new ResultOrderListDto();
        result.Filter = filter;
        foreach (OrderStatus status in Enum.GetValues<OrderStatus>())
        {
            result.StatusCounts[status] = orders.Count(o => o.Status == status);
        }

        if (filter.Status != null)
        {
            orders = orders.Where(o => o.Status == filter.Status).ToList();
        }
        if (filter.Channel != null)
        {
            orders = orders.Where(o => o.Channel == filter.Channel).ToList();
        }
        if (!string.IsNullOrWhiteSpace(filter.Q))
        {
            string q = filter.Q.Trim();
            orders = orders.Where(o =>
                o.OrderNo.Contains(q, StringComparison.OrdinalIgnoreCase)
                || (o.CustomerFullName != null && o.CustomerFullName.Contains(q, StringComparison.CurrentCultureIgnoreCase))
                || (o.CustomerPhone != null && o.CustomerPhone.Contains(q))).ToList();
        }

        // Sayfalama: 23 sipariş → 3 sayfa. Sayfa numarası 1 ile son sayfa arasına sıkıştırılır.
        result.Total = orders.Count;
        result.TotalPages = Math.Max(1, (int)Math.Ceiling(orders.Count / (double)pageSize));
        filter.Page = Math.Clamp(filter.Page, 1, result.TotalPages);
        result.Orders = orders.Skip((filter.Page - 1) * pageSize).Take(pageSize).ToList();
        return result;
    }

    /// <summary>
    /// Admin panel sayfasının bütün rakamları: bugünkü sipariş ve ciro, açık sipariş sayısı, son 7 günün cirosu,
    /// durumlara göre sayılar, en çok satan 5 ürün ve son 6 sipariş. İptal edilen siparişler ciroya katılmaz.
    /// </summary>
    public async Task<ResultDashboardDto> GetDashboardAsync()
    {
        var orders = await GetAllAsync();
        DateTime today = DateTime.Today;
        DateTime weekStart = today.AddDays(-6);
        List<ResultOrderDto> notCancelled = orders.Where(o => o.Status != OrderStatus.Cancelled).ToList();

        ResultDashboardDto dashboard = new ResultDashboardDto();
        dashboard.TodayOrders = orders.Count(o => o.CreatedAt >= today);
        dashboard.TodayRevenue = notCancelled.Where(o => o.CreatedAt >= today).Sum(o => o.Total);
        dashboard.WeekRevenue = notCancelled.Where(o => o.CreatedAt >= weekStart).Sum(o => o.Total);
        dashboard.OpenOrders = orders.Count(o => o.Status == OrderStatus.Received || o.Status == OrderStatus.Preparing || o.Status == OrderStatus.Ready);
        foreach (OrderStatus status in Enum.GetValues<OrderStatus>())
        {
            dashboard.StatusCounts[status] = orders.Count(o => o.Status == status);
        }

        // En çok satanlar: iptal edilmemiş siparişlerin bütün kalemleri ürün adına göre toplanır.
        dashboard.BestSellers = notCancelled
            .SelectMany(o => o.Details)
            .GroupBy(d => d.ProductName)
            .Select(g => new ResultBestSellerDto { Name = g.Key, Quantity = g.Sum(d => d.Quantity), Revenue = g.Sum(d => d.LineTotal) })
            .OrderByDescending(r => r.Quantity)
            .Take(5)
            .ToList();

        dashboard.LatestOrders = orders.Take(6).ToList();
        return dashboard;
    }

    /// <summary>Siparişi sipariş numarasıyla getirir (müşterinin onay sayfası). Yoksa null.</summary>
    public async Task<ResultOrderDto?> GetByOrderNoAsync(string orderNo)
    {
        var order = await OrdersWithDetails().FirstOrDefaultAsync(o => o.OrderNo == orderNo);
        if (order == null)
        {
            return null;
        }
        return _mapper.Map<ResultOrderDto>(order);
    }

    /// <summary>Verilen durumdaki sipariş sayısını SQL COUNT ile döndürür.</summary>
    public async Task<int> CountByStatusAsync(OrderStatus status)
    {
        return await _orderRepository.CountAsync(o => o.Status == status);
    }

    // ---------------- Fiyat hesabı ----------------

    /// <summary>
    /// Sepet sayfası ve sepet paneli için: sepeti fiyatlandırır ama KAYDETMEZ.
    /// </summary>
    public async Task<ResultOrderDto> CalculateAsync(OrderChannel channel, List<CartItemDto> items)
    {
        Order order = new Order();
        order.Channel = channel;
        await CalculateOrderAsync(order, items);
        return _mapper.Map<ResultOrderDto>(order);
    }

    /// <summary>
    /// Asıl hesap. Sepet sayfası, sepet paneli ve sipariş kaydı aynı hesabı kullanır.
    /// Tarayıcıdan gelen fiyat hiç kullanılmaz; fiyatlar veritabanından okunur.
    /// </summary>
    private async Task CalculateOrderAsync(Order order, List<CartItemDto> items)
    {
        // 1) Sepetteki ürünleri ve bütün seçenekleri veritabanından al.
        List<int> productIds = new List<int>();
        foreach (CartItemDto item in items)
        {
            productIds.Add(item.ProductId);
        }

        var products = await _productRepository.GetQueryable()
            .Include(p => p.Category)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        var allOptions = await _productOptionRepository.GetAllAsync();

        // 2) Her sepet satırı için bir sipariş kalemi oluştur.
        order.Details.Clear();
        foreach (CartItemDto item in items)
        {
            Product? product = products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product == null)
            {
                continue;   // ürün silinmiş: atla
            }

            List<ProductOption> chosenOptions = allOptions.Where(o => item.OptionIds.Contains(o.Id)).ToList();
            decimal unitPrice = CalculateUnitPrice(product, chosenOptions);

            OrderDetail detail = new OrderDetail();
            detail.Product = product;
            detail.ProductId = product.Id;
            detail.ProductName = product.Name;      // o anki adın kopyası
            detail.BasePrice = product.BasePrice;   // o anki fiyatın kopyası
            detail.UnitPrice = unitPrice;
            detail.Quantity = item.Quantity;
            detail.LineTotal = unitPrice * item.Quantity;

            foreach (ProductOption option in chosenOptions)
            {
                OrderDetailOption detailOption = new OrderDetailOption();
                detailOption.ProductOptionId = option.Id;
                detailOption.GroupKey = option.GroupKey;
                detailOption.Value = option.Value;
                detailOption.PriceDelta = option.PriceDelta;
                detail.Options.Add(detailOption);
            }

            order.Details.Add(detail);
        }

        // 3) Ara toplam
        order.Subtotal = order.Details.Sum(d => d.LineTotal);

        // 4) STRATEGY: sipariş türüne göre ücret kuralı seçilir.
        if (order.Details.Count == 0)
        {
            order.DeliveryFee = 0;
        }
        else
        {
            IDeliveryFeeStrategy feeStrategy = GetFeeStrategy(order.Channel);
            order.DeliveryFee = feeStrategy.Calculate(order.Subtotal);
        }

        order.Total = order.Subtotal + order.DeliveryFee;
    }

    /// <summary>DECORATOR: taban fiyatın üstüne her seçenek bir kat ekler.</summary>
    private decimal CalculateUnitPrice(Product product, List<ProductOption> options)
    {
        IPriceComponent price = new BaseProductPrice(product.BasePrice);
        foreach (ProductOption option in options)
        {
            price = new OptionPriceDecorator(price, option.PriceDelta);
        }
        return price.GetPrice();
    }

    /// <summary>STRATEGY: kanala göre ücret sınıfını seçer.</summary>
    private IDeliveryFeeStrategy GetFeeStrategy(OrderChannel channel)
    {
        if (channel == OrderChannel.Delivery)
        {
            return new DeliveryFeeStrategy(_pricing);
        }
        if (channel == OrderChannel.Pickup)
        {
            return new PickupFeeStrategy();
        }
        return new TableFeeStrategy();
    }

    /// <summary>FACTORY METHOD: kanala göre siparişi kuracak sınıfı seçer.</summary>
    private OrderCreator GetCreator(OrderChannel channel)
    {
        if (channel == OrderChannel.Delivery)
        {
            return new DeliveryOrderCreator();
        }
        if (channel == OrderChannel.Pickup)
        {
            return new PickupOrderCreator();
        }
        return new TableOrderCreator();
    }

    // ---------------- Sipariş verme ----------------

    /// <summary>Sepet sayfasındaki "Place Order" düğmesi buraya gelir.</summary>
    public async Task<ResultOrderDto> PlaceOrderAsync(CreateOrderDto dto, List<CartItemDto> items, string? qrToken)
    {
        // DTO → Entity: adres, not, alma saati, sipariş türü kopyalanır.
        Order form = _mapper.Map<Order>(dto);

        // Masa siparişinde masa QR koduyla bulunur; müşteri masa numarası yazamaz.
        Table? table = null;
        if (form.Channel == OrderChannel.Table && !string.IsNullOrWhiteSpace(qrToken))
        {
            string token = qrToken.ToUpper();
            table = await _tableRepository.GetFirstAsync(t => t.QrToken == token && t.IsActive);
        }

        // Müşteri: aynı telefonla daha önce sipariş veren müşteri tekrar oluşturulmaz.
        Customer customer = new Customer();
        customer.FullName = (dto.FullName ?? "").Trim();
        customer.Phone = (dto.Phone ?? "").Trim();
        if (customer.Phone != "")
        {
            string phone = customer.Phone;
            var existing = await _customerRepository.GetFirstAsync(c => c.Phone == phone);
            if (existing != null)
            {
                customer = existing;
            }
        }
        form.Customer = customer;

        // 1) FACTORY METHOD: türe uygun sipariş nesnesi kurulur.
        OrderCreator creator = GetCreator(form.Channel);
        Order order = creator.Create(form, table);

        // 2) DECORATOR + STRATEGY: fiyatlar ve ücret hesaplanır.
        await CalculateOrderAsync(order, items);

        // 3) CHAIN OF RESPONSIBILITY: kontroller sırayla; biri hata bulursa sipariş kaydedilmez.
        ProductHandler chain = new ProductHandler();
        chain.SetNext(new QuantityHandler())
             .SetNext(new ChannelInfoHandler())
             .SetNext(new OrderAmountHandler(_pricing));
        chain.Handle(order);

        // 4) Kayıt: sipariş, kalemler ve ilk durum kaydı tek transaction'da yazılır.
        await _orderRepository.CreateWithTransactionAsync(order);

        // 5) OBSERVER: mutfak, kasa ve admin haberdar edilir.
        await _events.OrderPlacedAsync(order);

        return _mapper.Map<ResultOrderDto>(order);
    }

    // ---------------- Durum değiştirme ----------------

    /// <summary>Admin sipariş detayındaki durum düğmeleri buraya gelir.</summary>
    public async Task ChangeStatusAsync(int id, OrderStatus target, string? note)
    {
        var order = await OrdersWithDetails().FirstOrDefaultAsync(o => o.Id == id);
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
        history.ChangedAt = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(note))
        {
            history.Note = note.Trim();
        }
        order.StatusHistories.Add(history);

        await _orderRepository.UpdateAsync(order);

        // OBSERVER: ilgililere haber ver.
        await _events.StatusChangedAsync(order, previous);
    }

    /// <summary>Siparişi müşteri, masa, kalemler, seçenekler ve durum geçmişiyle birlikte getiren sorgu.</summary>
    private IQueryable<Order> OrdersWithDetails()
    {
        return _orderRepository.GetQueryable()
            .Include(o => o.Customer)
            .Include(o => o.Table)
            .Include(o => o.Details).ThenInclude(d => d.Options)
            .Include(o => o.StatusHistories);
    }
}
