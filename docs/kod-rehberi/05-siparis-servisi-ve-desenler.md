# 05 · OrderService ve 6 tasarım deseni (projenin kalbi)

> Bu bölüm en uzun ve en önemli bölüm. Sipariş servislerini (`OrderService`, `OrderPricingService`, `OrderQueryService`, `DashboardService`) baştan sona satır satır okuyoruz ve her desene geldiğimizde o desenin bütün sınıflarını açıyoruz. Desen sınıflarının hepsi **DI'dan** geliyor; hangisinin nasıl kaydedildiği her desenin altında yazılı. Örnek olarak bugün test ettiğimiz gerçek bir siparişi izliyoruz:
>
> **CF-2936596E** · Teslimat · Ayşe Yılmaz · 2 × Vanilyalı Latte (Büyük + Yulaf Sütü + Shot), 1 × Vanilyalı Latte (Küçük + Normal Süt + Vanilya Şurubu), 1 × Tereyağlı Kruvasan

---

## 1. Sipariş işleri dört servise bölünmüş

Eskiden her şey tek bir `OrderService`'teydi: listeleme, panel rakamları, fiyat hesabı, sipariş verme ve durum değiştirme. 8 bağımlılığı vardı ve desen sınıflarını kendi içinde `new`'liyordu. Şimdi her sınıfın tek bir sorumluluğu var (Single Responsibility):

| Servis | Ne yapar | Kim kullanır |
|---|---|---|
| `IOrderPricingService` / `OrderPricingService` | Sepeti fiyatlandırır: **Decorator** + **Strategy**. Kaydetmez. | `CartService`, `CartDrawerViewComponent`, `OrderService` |
| `IOrderQueryService` / `OrderQueryService` | Okuma: admin listesi, detay (+ **State** ile izinli durumlar), onay sayfası, sayılar | `OrdersController`, `CartController`, `AdminSidebarViewComponent`, `DashboardService` |
| `IOrderService` / `OrderService` | Değiştirme: sipariş verme ve durum değiştirme. Desenleri sırayla çalıştırır. | `CartService`, `OrdersController` |
| `IDashboardService` / `DashboardService` | Admin panel rakamları | `DashboardController` |

`OrderService`'in bağımlılıkları artık şöyle:
```csharp
public OrderService(
    IOrderRepository orderRepository,
    ITableRepository tableRepository,
    ICustomerRepository customerRepository,
    IEnumerable<OrderCreator> creators,          // FACTORY METHOD: bütün creator'lar
    IOrderPricingService pricing,                // DECORATOR + STRATEGY
    IOrderValidationChain validation,            // CHAIN OF RESPONSIBILITY
    IOrderEventPublisher events,                 // OBSERVER
    IUnitOfWork unitOfWork,                      // tek SaveChanges
    TimeProvider time,
    IMapper mapper)
```
Sayı hâlâ 10. Ama bu sefer her bağımlılık **bir rol**: "sipariş vermek için gereken parçalar". Hiçbirini kendisi oluşturmuyor, hepsi DI'dan geliyor. Bu yüzden testte her birinin yerine sahte bir sürüm verilebilir. Bir **orkestratör** (işi kendisi yapmayan, parçaları sırayla çağıran sınıf) için bu normal.

`IOrderService`'in sözleşmesi artık sadece iki metot:
```csharp
Task<ResultOrderDto> PlaceOrderAsync(CreateOrderDto dto, List<CartItemDto> items, string? qrToken);
Task ChangeStatusAsync(int id, OrderStatus target, string? note);
```

---

## 2. Okuma: `OrderQueryService`

```csharp
public async Task<ResultOrderDto?> GetByIdAsync(int id)
{
    var order = await _orderRepository.GetWithDetailsAsync(id);
    if (order == null) return null;
    ResultOrderDto dto = _mapper.Map<ResultOrderDto>(order);
    dto.AllowedNextStatuses = OrderState.For(order.Status).GetAllowedNext();   // STATE: izinli sonraki durumlar
    return dto;
}

// OrderRepository
public async Task<Order?> GetWithDetailsAsync(int id) => await WithDetails().FirstOrDefaultAsync(o => o.Id == id);

private IQueryable<Order> WithDetails()
{
    return Set
        .Include(o => o.Customer)
        .Include(o => o.Table)
        .Include(o => o.Details).ThenInclude(d => d.Options)
        .Include(o => o.StatusHistories);
}
```
`ThenInclude`: iki seviye derin yükleme (Order → Details → Options). `WithDetails()` üç yerde kullanılıyor: detay, onay sayfası (`GetWithDetailsByOrderNoAsync`) ve durum değiştirme. Sorgu tekrar yazılmasın diye ayrı metoda alınmış (DRY: Don't Repeat Yourself). EF Core kodu (`Include`) sadece repository'de.

`GetByOrderNoAsync` aynı mantıkla çalışıyor. `CartController.Confirmation` bunu `orderNo.ToUpper()` ile çağırıyor.

### Admin listesi: filtre, sayma ve sayfalama SQL'de
```csharp
public async Task<ResultOrderListDto> GetAdminListAsync(OrderFilterDto filter)
{
    ResultOrderListDto result = new ResultOrderListDto();
    result.Filter = filter;
    result.StatusCounts = await GetStatusCountsAsync();                    // SQL GROUP BY

    result.Total = await _orderRepository.CountFilteredAsync(filter.Status, filter.Channel, filter.Q);   // SQL COUNT
    result.TotalPages = Math.Max(1, (int)Math.Ceiling(result.Total / (double)PageSize));
    filter.Page = Math.Clamp(filter.Page, 1, result.TotalPages);

    var orders = await _orderRepository.GetFilteredPageAsync(
        filter.Status, filter.Channel, filter.Q, (filter.Page - 1) * PageSize, PageSize);   // SQL OFFSET/FETCH
    result.Orders = _mapper.Map<List<ResultOrderDto>>(orders);
    return result;
}
```
**Eskiden** bu metot önce `GetAllAsync()` ile **bütün** siparişleri müşteri, masa ve kalemleriyle belleğe çekiyor, sonra sayma, filtreleme ve sayfalamayı C#'ta yapıyordu. 18 siparişte fark edilmez; 100.000 siparişte her liste açılışında 100.000 sipariş belleğe gelirdi. Şimdi veritabanından sadece **bu sayfanın 10 siparişi** geliyor; sayılar ise SQL'de hesaplanıp tek bir rakam olarak dönüyor.

```csharp
public async Task<Dictionary<OrderStatus, int>> GetStatusCountsAsync()
{
    var counts = await _orderRepository.GetStatusCountsAsync();   // sadece siparişi olan durumlar
    Dictionary<OrderStatus, int> result = new Dictionary<OrderStatus, int>();
    foreach (OrderStatus status in Enum.GetValues<OrderStatus>())
        result[status] = counts.GetValueOrDefault(status);         // olmayan durum → 0
    return result;
}
```
SQL'deki `GROUP BY` sadece en az bir siparişi olan durumları döndürür. Hiç iptal edilmiş sipariş yoksa `Cancelled` satırı gelmez. Ekrandaki kartlar her durumu göstermek istediği için eksik durumlar 0 ile dolduruluyor.

```csharp
public async Task<int> CountByStatusAsync(OrderStatus status)
    => await _orderRepository.CountAsync(o => o.Status == status);
```
Sidebar'daki "Siparişler 1" rozeti: `Received` sayısı.

---

## 3. Fiyat hesabı: `OrderPricingService`

### Sepet için: kaydetmeden hesapla
```csharp
public async Task<ResultOrderDto> CalculateAsync(OrderChannel channel, List<CartItemDto> items)
{
    Order order = new Order();
    order.Channel = channel;
    await ApplyPricesAsync(order, items);
    return _mapper.Map<ResultOrderDto>(order);
}
```
Sepet sayfası (`CartService`) ve sağdan açılan sepet paneli (`CartDrawerViewComponent`) bu metodu çağırıyor. Bellekte geçici bir `Order` oluşuyor, hesaplanıyor, DTO'ya çevrilip ekrana gidiyor. **Hiçbir zaman veritabanına yazılmıyor** (repository'ye verilmiyor).

**Asıl tasarım fikri:** sepet ekranı ve gerçek sipariş **aynı** `ApplyPricesAsync`'i kullanıyor. `OrderService.PlaceOrderAsync` de fiyat için `IOrderPricingService.ApplyPricesAsync`'i çağırıyor. Kullanıcının sepette gördüğü tutar ile kaydedilen tutar asla farklı olamaz, çünkü iki ayrı hesap kodu yok.

### ApplyPricesAsync: adım adım

```csharp
public async Task ApplyPricesAsync(Order order, List<CartItemDto> items)
{
    // 1) Sepetteki ürünleri ve bütün seçenekleri veritabanından al.
    var products = await _productRepository.GetByIdsWithCategoryAsync(items.Select(i => i.ProductId));
    var allOptions = await _productOptionRepository.GetAllAsync();
```
- `GetByIdsWithCategoryAsync` repository'de `ids.Distinct()` yapıp `WHERE Id IN (2, 14)` sorgusu çalıştırıyor. Sepetteki bütün ürünler **tek sorguda** geliyor. Her sepet satırı için ayrı sorgu atılsaydı bu "N+1 problemi" olurdu.
- Repository `.Include(p => p.Category)` da yapıyor. İki işe yarıyor: (a) Chain'deki `ProductHandler` `detail.Product.Category.IsActive`'e bakıyor, (b) sepette kategori adı gösteriliyor.
- **Dikkat:** Burada `IsActive` filtresi **yok**. Pasif yapılmış bir ürün sepette kalıyor ve hesaplanıyor; siparişi engelleyen şey Chain'deki `ProductHandler` ("... artık satışta değil."). Bu bilinçli: kullanıcı ürünün neden gittiğini mesajla görüyor.
- `allOptions`: seçeneklerin hepsi tek sorguda geliyor (küçük bir tablo).

CF-2936596E için: `ProductId`'ler `[2, 2, 14]` → 2 ürün geldi (Vanilyalı Latte, Tereyağlı Kruvasan).

```csharp
    // 2) Her sepet satırı için bir sipariş kalemi oluştur.
    order.Details.Clear();
    foreach (CartItemDto item in items)
    {
        Product? product = products.FirstOrDefault(p => p.Id == item.ProductId);
        if (product == null) continue;   // ürün silinmiş: atla
```
- `Details.Clear()`: metot iki kez çağrılırsa kalemler ikiye katlanmasın diye.
- `products.FirstOrDefault(...)` artık **RAM'de** çalışıyor (`products` bir `List`). SQL'e gitmiyor.
- Silinmiş ürün sessizce atlanıyor. `CartService.GetCartAsync` de bu satırı sepetten çıkarıyor; controller temizlenmiş sepeti Session'a geri yazıyor.

```csharp
        List<ProductOption> chosenOptions = allOptions.Where(o => item.OptionIds.Contains(o.Id)).ToList();
        decimal unitPrice = CalculateUnitPrice(product, chosenOptions);   // ← DECORATOR
```
- Var olmayan bir seçenek Id'si gönderilirse (form manipülasyonu) `allOptions` içinde bulunamaz ve sessizce yok sayılır.
- Aynı Id iki kez gönderilse bile (`[5,5]`) seçenek **bir kez** sayılır, çünkü dönülen liste `allOptions` ve her seçenek orada bir kez var. Fiyat iki kez eklenemez.

CF-2936596E 1. satır: `OptionIds = [3, 5, 7]` → Büyük(+30), Yulaf Sütü(+15), Shot(+20).

```csharp
        OrderDetail detail = new OrderDetail();
        detail.Product = product;
        detail.ProductId = product.Id;
        detail.ProductName = product.Name;      // o anki adın kopyası
        detail.BasePrice = product.BasePrice;   // o anki fiyatın kopyası
        detail.UnitPrice = unitPrice;
        detail.Quantity = item.Quantity;
        detail.LineTotal = unitPrice * item.Quantity;
```
**`detail.Product = product` satırındaki çok ince bir EF Core ayrıntısı:**
`product` bu istekte **aynı DbContext** tarafından yüklendi, dolayısıyla change tracker onu `Unchanged` olarak tanıyor. Sipariş kaydedilirken `AddAsync(order)` bütün grafiği gezer: yeni nesneleri `Added` yapar, zaten izlenen `product`'a ise dokunmaz. Eğer `product` başka bir DbContext'ten gelmiş (veya `AsNoTracking` ile okunmuş) olsaydı, EF onu yeni bir ürün sanıp **INSERT etmeye çalışırdı** ve çift kayıt ya da hata olurdu. `OrderPricingService` ayrı bir servis olsa da aynı scope'ta, dolayısıyla aynı DbContext'i kullanan repository'leri alıyor (Bölüm 01). Bu kod o sayede doğru çalışıyor.

```csharp
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
```
Seçenekler kopyalanıyor (snapshot). `detailOption.ProductOption` navigation'ı atanmıyor, sadece Id veriliyor. İkisi de geçerli bir yöntem.

```csharp
    // 3) Ara toplam
    order.Subtotal = order.Details.Sum(d => d.LineTotal);

    // 4) STRATEGY
    order.DeliveryFee = order.Details.Count == 0 ? 0 : GetFeeStrategy(order.Channel).Calculate(order.Subtotal);
    order.Total = order.Subtotal + order.DeliveryFee;
}
```
CF-2936596E: 450 + 170 + 95 = **715** → teslimat, 715 ≥ 600 → ücret **0** → toplam **715**.

---

## 4. DESEN 1: DECORATOR (fiyata seçenek eklemek)

### Sınıflar
```csharp
// Pricing/IPriceComponent.cs
public interface IPriceComponent
{
    decimal GetPrice();
}

// Pricing/BaseProductPrice.cs: en içteki kutu (ConcreteComponent)
public class BaseProductPrice : IPriceComponent
{
    private readonly decimal _basePrice;
    public BaseProductPrice(decimal basePrice) { _basePrice = basePrice; }
    public decimal GetPrice() { return _basePrice; }
}

// Pricing/OptionPriceDecorator.cs: saran kutu (Decorator)
public class OptionPriceDecorator : IPriceComponent
{
    private readonly IPriceComponent _inner;     // ← sardığı nesne (aynı arayüz!)
    private readonly decimal _extraPrice;
    public OptionPriceDecorator(IPriceComponent inner, decimal extraPrice) { _inner = inner; _extraPrice = extraPrice; }
    public decimal GetPrice() { return _inner.GetPrice() + _extraPrice; }
}
```

### Kullanım
```csharp
private decimal CalculateUnitPrice(Product product, List<ProductOption> options)
{
    IPriceComponent price = new BaseProductPrice(product.BasePrice);
    foreach (ProductOption option in options)
        price = new OptionPriceDecorator(price, option.PriceDelta);
    return price.GetPrice();
}
```

### CF-2936596E 1. satır için bellekte oluşan yapı
```text
price = BaseProductPrice(160)
price = OptionPriceDecorator(inner: BaseProductPrice(160), +30)
price = OptionPriceDecorator(inner: OptionPriceDecorator(...+30), +15)
price = OptionPriceDecorator(inner: OptionPriceDecorator(...+15), +20)

price.GetPrice()
  → (+20).GetPrice() = inner.GetPrice() + 20
       → (+15).GetPrice() = inner.GetPrice() + 15
            → (+30).GetPrice() = inner.GetPrice() + 30
                 → BaseProductPrice.GetPrice() = 160
            ← 190
       ← 205
  ← 225   ✔ (veritabanında UnitPrice = 225.00)
```

### GoF terimleri
| GoF rolü | Bu projede |
|---|---|
| Component | `IPriceComponent` |
| ConcreteComponent | `BaseProductPrice` |
| Decorator | `OptionPriceDecorator` |

Klasik GoF'ta bir de soyut `Decorator` sınıfı ve birden fazla `ConcreteDecorator` (`MilkDecorator`, `ShotDecorator`...) olur. Burada tek bir decorator var, çünkü bütün seçeneklerin davranışı aynı: sabit bir tutar eklemek. Fark verinin kendisinde (`PriceDelta`), davranışta değil.

### Nasıl genişletilir?
"Büyük boy fiyatı %20 artırsın" kuralı gelse:
```csharp
public class PercentPriceDecorator : IPriceComponent
{
    private readonly IPriceComponent _inner; private readonly decimal _percent;
    public decimal GetPrice() => _inner.GetPrice() * (1 + _percent / 100m);
}
```
Sıra artık önem kazanır: %20'yi shot eklenmeden önce mi sonra mı uygulayacaksın? Decorator'da **sarma sırası sonucu değiştirebilir**. Toplamada fark etmez, çarpmada eder.

### Decorator'ı ne zaman kullanmazsın?
Sadece "toplam = taban + seçeneklerin toplamı" gerekiyorsa `product.BasePrice + options.Sum(o => o.PriceDelta)` tek satırda aynı sonucu verir. Decorator'ın değeri, **farklı davranışlar üst üste bindiğinde** ortaya çıkar. Sınavda "neden basit Sum değil?" sorusuna cevabın: *"Seçenek davranışları çeşitlenebilir (yüzde, koşullu indirim, üst sınır). Decorator her birini ayrı bir sınıf olarak ekleme imkânı veriyor ve hesap kodu değişmiyor."*

---

## 5. DESEN 2: STRATEGY (kanala göre ücret)

### Sınıflar
```csharp
// Pricing/Fees/IDeliveryFeeStrategy.cs
public interface IDeliveryFeeStrategy
{
    OrderChannel Channel { get; }              // bu strateji hangi kanal için?
    decimal Calculate(decimal orderAmount);
}

// Pricing/Fees/DeliveryFeeStrategy.cs
public class DeliveryFeeStrategy : IDeliveryFeeStrategy
{
    private readonly PricingOptions _options;
    public DeliveryFeeStrategy(IOptions<PricingOptions> options) { _options = options.Value; }
    public OrderChannel Channel => OrderChannel.Delivery;
    public decimal Calculate(decimal orderAmount)
    {
        if (orderAmount >= _options.FreeDeliveryFrom) return 0;   // 600 ve üzeri ücretsiz
        return _options.DeliveryFee;                              // 30
    }
}

// Pricing/Fees/PickupFeeStrategy.cs
public class PickupFeeStrategy : IDeliveryFeeStrategy
{
    public OrderChannel Channel => OrderChannel.Pickup;
    public decimal Calculate(decimal orderAmount) { return 0; }
}

// Pricing/Fees/TableFeeStrategy.cs
public class TableFeeStrategy : IDeliveryFeeStrategy
{
    public OrderChannel Channel => OrderChannel.Table;
    public decimal Calculate(decimal orderAmount) { return 0; }
}

// Pricing/PricingOptions.cs
public class PricingOptions
{
    public const string SectionName = "Pricing";
    [Range(0, 10000)]  public decimal DeliveryFee { get; set; } = 30;
    [Range(0, 100000)] public decimal FreeDeliveryFrom { get; set; } = 600;
    [Range(0, 100000)] public decimal MinDeliveryAmount { get; set; } = 100;   // Chain'deki OrderAmountHandler kullanır
}
```
Her strateji **hangi kanal için olduğunu kendisi söylüyor** (`Channel` property'si). Ayarları `IOptions<PricingOptions>` ile alıyor (Bölüm 01, Options pattern).

### Seçim (Context): DI + sözlük
```csharp
// BusinessServiceRegistration
services.AddSingleton<IDeliveryFeeStrategy, DeliveryFeeStrategy>();
services.AddSingleton<IDeliveryFeeStrategy, PickupFeeStrategy>();
services.AddSingleton<IDeliveryFeeStrategy, TableFeeStrategy>();

// OrderPricingService
public OrderPricingService(..., IEnumerable<IDeliveryFeeStrategy> feeStrategies, ...)
{
    _feeStrategies = feeStrategies.ToDictionary(s => s.Channel);   // Delivery → ..., Pickup → ..., Table → ...
}

private IDeliveryFeeStrategy GetFeeStrategy(OrderChannel channel)
{
    if (_feeStrategies.TryGetValue(channel, out IDeliveryFeeStrategy? strategy))
        return strategy;
    throw new InvalidOperationException(channel + " kanalı için IDeliveryFeeStrategy kaydı yok.");
}
```
- `IEnumerable<IDeliveryFeeStrategy>`: DI kayıtlı **bütün** stratejileri verir (Bölüm 01, "aynı arayüze birden fazla kayıt").
- `ToDictionary(s => s.Channel)`: kanaldan stratejiye bir sözlük. Seçim artık `if` zinciri değil, sözlükte arama.
- **Erken hata:** Aynı kanal için iki strateji kaydedilirse `ToDictionary` "aynı anahtar zaten var" hatası verir. Bir kanalın stratejisi unutulursa ilk hesapta, ne eksik olduğunu söyleyen bir mesajla durur. Eskiden `if` zincirinin sonundaki `return new TableFeeStrategy();` bilinmeyen her kanalı sessizce "ücretsiz" sayardı.

| GoF rolü | Bu projede |
|---|---|
| Strategy | `IDeliveryFeeStrategy` |
| ConcreteStrategy | `DeliveryFeeStrategy`, `PickupFeeStrategy`, `TableFeeStrategy` |
| Context | `OrderPricingService.ApplyPricesAsync` (stratejiyi kullanan) |

### Test sonuçları
| Sipariş | Ara toplam | Strateji | Ücret |
|---|---|---|---|
| CF-28ED1FE2 (Zeynep, teslimat) | 275 | Delivery, 275 < 600 | **30** → toplam 305 |
| CF-2936596E (Ayşe, teslimat) | 715 | Delivery, 715 ≥ 600 | **0** |
| CF-91F40FE4 (Mehmet, gel-al) | 705 | Pickup | 0 |
| CF-92B1EF1D (Masa 7) | 885 | Table | 0 |

### Eskiden nasıldı, neden değişti?
```csharp
// ESKİ: OrderService içinde
private IDeliveryFeeStrategy GetFeeStrategy(OrderChannel channel)
{
    if (channel == OrderChannel.Delivery) return new DeliveryFeeStrategy(_pricing);
    if (channel == OrderChannel.Pickup)   return new PickupFeeStrategy();
    return new TableFeeStrategy();
}
```
İki sorunu vardı:
1. **Open/Closed ihlali:** yeni bir kanal (örneğin "Kurye-Express") eklemek için bu metoda dokunmak gerekiyordu.
2. **Bağımlılığı servis taşıyordu:** `DeliveryFeeStrategy`'nin `PricingOptions`'a ihtiyacı vardı, bu yüzden `OrderService` de `PricingOptions` istemek ve elle geçirmek zorundaydı. Bir stratejiye yeni bir bağımlılık eklenseydi servisin constructor'ı da değişecekti.

Şimdi yeni kanal = **yeni sınıf + `BusinessServiceRegistration`'a bir satır**. `OrderPricingService` değişmiyor. Observer'da da tam olarak bu teknik kullanılıyor.

**Alternatif: Keyed services (.NET 8).** `services.AddKeyedSingleton<IDeliveryFeeStrategy, DeliveryFeeStrategy>(OrderChannel.Delivery)` ile kayıt yapılıp `[FromKeyedServices(OrderChannel.Delivery)]` veya `IKeyedServiceProvider` ile alınabilirdi. Burada `Channel` property'si tercih edildi: anahtar sınıfın kendisinde yazılı, kayıt satırında ayrıca tekrarlanmıyor.

İsim notu: `PickupFeeStrategy` ve `TableFeeStrategy`'nin ikisi de 0 döndürüyor, ve arayüzün adı `IDeliveryFeeStrategy`. Aslında "kanal ücreti stratejisi" demek daha doğru olurdu. Ayrı sınıflar olmaları ise doğru: ileride "masa servisi %10" kuralı geldiğinde sadece `TableFeeStrategy` değişir.

---

## 6. `PlaceOrderAsync`: sipariş verme, baştan sona

```csharp
public async Task<ResultOrderDto> PlaceOrderAsync(CreateOrderDto dto, List<CartItemDto> items, string? qrToken)
{
    // DTO → Entity
    Order form = _mapper.Map<Order>(dto);
    form.Customer = await FindOrCreateCustomerAsync(dto.FullName, dto.Phone);
```
`form` **geçici** bir nesne: formdan gelen değerleri entity biçiminde taşıyor (`Channel`, `Address`, `AddressNotes`, `PickupTime`, `Note`). Kaydedilecek sipariş bu değil. Factory Method yeni bir `Order` oluşturacak ve gerekli alanları bundan kopyalayacak.

```csharp
    // Masa siparişinde masa QR koduyla bulunur; müşteri masa numarası yazamaz.
    Table? table = null;
    if (form.Channel == OrderChannel.Table && !string.IsNullOrWhiteSpace(qrToken))
    {
        string token = qrToken.ToUpper();
        table = await _tableRepository.GetFirstAsync(t => t.QrToken == token && t.IsActive);
    }
```
- `qrToken` Session'dan geliyor (`CartController`, QR okutulduğunda Session'a yazmıştı).
- QR **ikinci kez** doğrulanıyor: müşteri QR okuttuktan sonra admin masayı pasif yaptıysa sipariş anında yakalanır.
- Masa bulunamazsa `table = null` kalır. Hata **burada fırlatılmıyor**; Chain'deki `ChannelInfoHandler` yakalıyor ("Lütfen masanızdaki QR kodu okutun."). Hata mesajları tek bir yerden çıksın diye.

Müşteri, ayrı bir yardımcı metotta bulunuyor ya da oluşturuluyor:
```csharp
// Aynı telefonla daha önce sipariş veren müşteri tekrar oluşturulmaz.
private async Task<Customer> FindOrCreateCustomerAsync(string? fullName, string? phone)
{
    Customer customer = new Customer();
    customer.FullName = (fullName ?? "").Trim();
    customer.Phone = (phone ?? "").Trim();
    if (customer.Phone != "")
    {
        string p = customer.Phone;
        var existing = await _customerRepository.GetFirstAsync(c => c.Phone == p);
        if (existing != null) return existing;
    }
    return customer;   // yeni müşteri: henüz kaydedilmedi, sipariş grafiğiyle birlikte INSERT edilecek
}
```
İnce noktalar:
- **Telefon = müşteri kimliği.** Ayşe ikinci kez sipariş verdiğinde (`CF-ADF38C79`) yeni müşteri oluşmadı, `CustomerId = 1` tekrar kullanıldı. Bugünkü sorguda 4 sipariş için 3 müşteri görmemizin sebebi bu.
- Var olan müşteri bulunursa formda yazılan **ad yok sayılıyor** (`customer = existing`). Aynı telefonla farklı bir isim yazılsa bile kayıttaki eski isim kalır.
- `string p = customer.Phone;` neden ayrı bir değişkene alınmış? Lambda (`c => c.Phone == p`) içinde `customer.Phone` kullanılsaydı da çalışırdı. Ayrı değişken sorgunun neye bağlı olduğunu netleştiriyor; EF Core yerel değişkeni SQL parametresine çeviriyor.
- Masa siparişinde de bir `Customer` nesnesi oluşuyor (boş ad ve telefonla) ve `form.Customer`'a atanıyor. Ama `TableOrderCreator` müşteriyi **kopyalamıyor**, dolayısıyla bu nesne hiçbir zaman kaydedilmiyor. Masa siparişlerinde `CustomerId = NULL` olmasının sebebi bu.

```csharp
    // 1) FACTORY METHOD
    Order order = GetCreator(form.Channel).Create(form, table);
```

---

## 7. DESEN 3: FACTORY METHOD (kanala göre sipariş nesnesi)

### Soyut yaratıcı
```csharp
public abstract class OrderCreator
{
    private readonly TimeProvider _time;
    protected OrderCreator(TimeProvider time) { _time = time; }

    public abstract OrderChannel Channel { get; }   // bu creator hangi kanalın siparişini kurar?

    public Order Create(Order form, Table? table)
    {
        // 1) Türe özgü kısmı alt sınıf doldurur.
        Order order = CreateForChannel(form, table);

        // 2) Ortak kısım
        order.OrderNo = CreateOrderNo();
        order.Status = OrderStatus.Received;
        order.Note = form.Note;
        order.CreatedAt = _time.GetLocalNow().DateTime;

        // 3) İlk durum kaydı: "Alındı"
        OrderStatusHistory history = new OrderStatusHistory();
        history.ToStatus = OrderStatus.Received;      // FromStatus = null
        history.ChangedAt = order.CreatedAt;
        order.StatusHistories.Add(history);

        return order;
    }

    protected abstract Order CreateForChannel(Order form, Table? table);   // ← FACTORY METHOD

    private static string CreateOrderNo()
    {
        string guid = Guid.NewGuid().ToString("N");   // 32 hex karakter, tiresiz
        return "CF-" + guid.Substring(0, 8).ToUpper(); // CF-2936596E
    }
}
```
- `protected abstract`: dışarıdan çağrılamaz, alt sınıf yazmak **zorunda**.
- `Create` metodu `virtual` değil, alt sınıf onu ezemez. Ortak adımların sırası sabit kalır. Bu **Template Method** desenidir: iskelet üst sınıfta, değişen adım alt sınıfta.
- Sipariş numarası: GUID'in ilk 8 hex karakteri → 16⁸ ≈ 4.3 milyar olasılık. Çakışma ihtimali çok düşük; olursa `OrderNo` üzerindeki unique index INSERT'i reddeder ve exception olur (yeniden deneme yok). Rastgele olmasının bir faydası da var: sıralı bir numara (`CF-0001`) olsaydı biri `/cart/confirmation/CF-0002` yazıp başkasının siparişini görebilirdi.
- Saat `DateTime.Now` ile değil, DI'dan gelen `TimeProvider` ile alınıyor (Bölüm 01). Testte saat sabitlenebilir. Hâlâ sunucunun **yerel** saati kullanılıyor (`GetLocalNow`); birden fazla saat dilimi olsaydı `GetUtcNow()` ile UTC saklamak tercih edilirdi.
- `protected` constructor: soyut sınıf doğrudan oluşturulamaz; alt sınıflar `TimeProvider`'ı `: base(time)` ile yukarı iletiyor.

### Somut yaratıcılar
```csharp
public class DeliveryOrderCreator : OrderCreator
{
    public DeliveryOrderCreator(TimeProvider time) : base(time) { }
    public override OrderChannel Channel => OrderChannel.Delivery;

    protected override Order CreateForChannel(Order form, Table? table)
    {
        Order order = new Order();
        order.Channel = OrderChannel.Delivery;
        order.Customer = form.Customer;
        order.Address = form.Address;
        order.AddressNotes = form.AddressNotes;
        return order;
    }
}

public class PickupOrderCreator : OrderCreator
{
    public PickupOrderCreator(TimeProvider time) : base(time) { }
    public override OrderChannel Channel => OrderChannel.Pickup;

    protected override Order CreateForChannel(Order form, Table? table)
    {
        Order order = new Order();
        order.Channel = OrderChannel.Pickup;
        order.Customer = form.Customer;
        order.PickupTime = form.PickupTime;
        return order;
    }
}

public class TableOrderCreator : OrderCreator
{
    public TableOrderCreator(TimeProvider time) : base(time) { }
    public override OrderChannel Channel => OrderChannel.Table;

    protected override Order CreateForChannel(Order form, Table? table)
    {
        Order order = new Order();
        order.Channel = OrderChannel.Table;
        if (table != null) order.TableId = table.Id;
        return order;
    }
}
```
**Beyaz liste (whitelist) mantığı:** Her yaratıcı sadece kendi kanalına ait alanları kopyalıyor. Teslimat siparişine `PickupTime` gelmiş olsa bile kopyalanmaz. Masa siparişine adres gelmiş olsa bile kopyalanmaz. Kanala uymayan veri veritabanına giremez.

`Channel` de formdan değil, sınıfın kendisinden atanıyor (`OrderChannel.Delivery`). Bu bir tutarlılık garantisi: `DeliveryOrderCreator` her zaman bir teslimat siparişi üretir.

`table.Id` atanıyor ama `order.Table = table` değil. İkisi de olurdu; Id vermek yeterli.

### Seçici: DI + sözlük
```csharp
// BusinessServiceRegistration
services.AddSingleton<OrderCreator, DeliveryOrderCreator>();
services.AddSingleton<OrderCreator, PickupOrderCreator>();
services.AddSingleton<OrderCreator, TableOrderCreator>();

// OrderService
_creators = creators.ToDictionary(c => c.Channel);    // constructor'da, IEnumerable<OrderCreator>'dan

private OrderCreator GetCreator(OrderChannel channel)
{
    if (_creators.TryGetValue(channel, out OrderCreator? creator)) return creator;
    throw new InvalidOperationException(channel + " kanalı için OrderCreator kaydı yok.");
}
```
Strategy ile birebir aynı teknik (Bölüm 5): her creator kanalını `Channel` property'si ile söylüyor, DI hepsini liste olarak veriyor, servis sözlükten seçiyor. **Eskiden** burada `if (channel == ...) return new DeliveryOrderCreator();` zinciri vardı; yeni bir kanal için `OrderService`'e dokunmak gerekiyordu.

Neden **Singleton**? Creator'ların içinde değişen bir durum yok (`Create` her çağrıda yeni bir `Order` döndürüyor) ve tek bağımlılıkları singleton olan `TimeProvider`. Aynı nesne bütün isteklerde güvenle paylaşılabilir.

### GoF terimleri ve sınavda söylenecek cümle
| GoF rolü | Bu projede |
|---|---|
| Creator | `OrderCreator` |
| ConcreteCreator | `DeliveryOrderCreator`, `PickupOrderCreator`, `TableOrderCreator` |
| factoryMethod() | `CreateForChannel` |
| Product | `Order` |
| Creator'ın factory method'u kullanan işlemi | `Create` (Template Method) |

> *"Factory Method, `CreateForChannel`. `OrderCreator.Create` ortak adımları sabitleyip türe özgü üretimi alt sınıfa bırakıyor, yani Template Method ile birlikte çalışıyor. Creator'lar DI'a kayıtlı; `OrderService` hangisinin kullanılacağını kanala göre bir sözlükten seçiyor."*

Klasik Factory Method'da "Product" da çoğu zaman bir hiyerarşidir (`DeliveryOrder : Order`). Burada ürün tipi tek (`Order`), farklı olan **nasıl doldurulduğu**. Bu da geçerli ve yaygın bir kullanım.

---

```csharp
    // 2) DECORATOR + STRATEGY
    await _pricing.ApplyPricesAsync(order, items);
```
Yukarıda incelediğimiz hesap (`OrderPricingService`), bu sefer gerçek sipariş için çalışıyor. Fiyatlar veritabanından **yeniden** okunuyor; sepet sayfasında hesaplanan değerler hiç kullanılmıyor.

```csharp
    // 3) CHAIN OF RESPONSIBILITY
    _validation.Validate(order);
```
`_validation` bir `IOrderValidationChain`. Zincirin nasıl kurulduğunu `OrderService` bilmiyor.

---

## 8. DESEN 4: CHAIN OF RESPONSIBILITY (doğrulama zinciri)

### Soyut halka
```csharp
public abstract class OrderValidationHandler
{
    private OrderValidationHandler? _next;

    public OrderValidationHandler SetNext(OrderValidationHandler next)
    {
        _next = next;
        return next;
    }

    public void Handle(Order order)
    {
        Check(order);
        if (_next != null) _next.Handle(order);
    }

    protected abstract void Check(Order order);
}
```

### Zincirin kurulması: DI + `OrderValidationChain`
Halkalar DI'a kayıtlı. **Kayıt sırası zincir sırasıdır:**
```csharp
// BusinessServiceRegistration
services.AddTransient<OrderValidationHandler, ProductHandler>();       // 1
services.AddTransient<OrderValidationHandler, QuantityHandler>();      // 2
services.AddTransient<OrderValidationHandler, ChannelInfoHandler>();   // 3
services.AddTransient<OrderValidationHandler, OrderAmountHandler>();   // 4
services.AddScoped<IOrderValidationChain, OrderValidationChain>();
```
`OrderValidationChain` halkaları liste olarak alıp birbirine bağlıyor:
```csharp
public OrderValidationChain(IEnumerable<OrderValidationHandler> handlers)
{
    OrderValidationHandler? previous = null;
    foreach (OrderValidationHandler handler in handlers)   // kayıt sırasıyla: P, Q, C, A
    {
        if (previous == null) _first = handler;           // _first = P
        else previous.SetNext(handler);                   // P._next = Q, Q._next = C, C._next = A
        previous = handler;
    }
}
// Sonuç: P → Q → C → A → null

public void Validate(Order order) => _first?.Handle(order);   // P.Check → Q.Check → C.Check → A.Check
```
**Eskiden** zincir `OrderService` içinde `new` ile kuruluyordu:
```csharp
ProductHandler chain = new ProductHandler();
chain.SetNext(new QuantityHandler())                   // P._next = Q,  döner: Q
     .SetNext(new ChannelInfoHandler())                // Q._next = C,  döner: C
     .SetNext(new OrderAmountHandler(_pricing));       // C._next = A,  döner: A
```
Sorun: `OrderService`, her halkanın neye ihtiyaç duyduğunu (`_pricing`) bilmek zorundaydı ve yeni bir kural için servisin değişmesi gerekiyordu. Şimdi yeni bir kural = yeni bir halka sınıfı + kayıt listesine bir satır.

**Halkalar neden Transient?** `SetNext` halkanın içindeki `_next` alanını **değiştiriyor**. Halkalar Singleton olsaydı her istekte aynı nesneler tekrar tekrar bağlanırdı; iki istek aynı anda zincir kurarken birbirinin bağlantısını bozabilirdi. Transient ile her `OrderValidationChain` kendi halka nesnelerini alıyor.

`SetNext` hâlâ `return next;` yapıyor (eski "yan yana yazma" kullanımı için). `this` döndürseydi eski yazımda: `P._next = Q`, sonra `P._next = C` (Q'nun üstüne yazılır!), sonra `P._next = A`. Zincir **P → A** olurdu ve Q ile C hiç çalışmazdı. `OrderValidationChain` dönüş değerini kullanmadığı için bu tuzaktan etkilenmiyor.

`Handle` ve `Check` ayrımı: `Handle` zinciri yürüten ortak mantık (alt sınıflar ezemez, `virtual` değil). `Check` her halkanın kendi kuralı. Yine bir Template Method ilişkisi.

### Halka 1: ProductHandler
```csharp
protected override void Check(Order order)
{
    if (order.Details.Count == 0)
        throw new BusinessException("Your cart is empty.");

    foreach (OrderDetail detail in order.Details)
    {
        if (!detail.Product.IsActive || !detail.Product.Category.IsActive)
            throw new BusinessException(detail.Product.Name + " artık satışta değil.");

        if (detail.Options.Count > 0 && !detail.Product.IsCustomizable)
            throw new BusinessException(detail.Product.Name + " cannot be customized.");

        List<string> usedGroups = new List<string>();
        foreach (OrderDetailOption option in detail.Options)
        {
            if (usedGroups.Contains(option.GroupKey))
                throw new BusinessException("Only one choice is allowed per option group.");
            usedGroups.Add(option.GroupKey);
        }
    }
}
```
- `detail.Product.Category.IsActive`: `ProductRepository.GetByIdsWithCategoryAsync`'teki `Include(p => p.Category)` olmasaydı burada `NullReferenceException` olurdu.
- **Grup kontrolü:** formu elle değiştirip `sizeId=1&sizeId=3` (hem Küçük hem Büyük) veya Yulaf + Badem sütü gönderen biri burada durdurulur. Bu kontrol olmasaydı iki süt birden ücretlendirilirdi.
- "Özelleştirilemeyen ürüne seçenek" kontrolü ikinci bir savunma hattı. İlki `CartController.Add`'te: `if (product.IsCustomizable)` değilse seçenekler sepete hiç eklenmiyor. Bugünkü testte Lotus Cheesecake'e `optionIds=7` gönderdik; controller seçeneği attı ve sipariş seçeneksiz kaydedildi (CF-0000F5A1). Yani bu halkaya hiç ulaşmadı, ama controller değişirse burası yakalar (savunma derinliği).

### Halka 2: QuantityHandler
```csharp
foreach (OrderDetail detail in order.Details)
    if (detail.Quantity < 1 || detail.Quantity > 20)
        throw new BusinessException("Quantity must be between 1 and 20.");
```
Controller da adedi 1–20 aralığına sıkıştırıyor (`Math.Min(20, ...)`). Bu halka yine ikinci savunma.

### Halka 3: ChannelInfoHandler
```csharp
// constructor: public ChannelInfoHandler(TimeProvider time) { _time = time; }   (saat DI'dan)
if (order.Channel == OrderChannel.Table)
{
    if (order.TableId == null) throw new BusinessException("Lütfen masanızdaki QR kodu okutun.");
    return;     // ← masa siparişinde ad/telefon sorulmaz, erken çık
}
if (order.Customer == null || string.IsNullOrWhiteSpace(order.Customer.FullName) || string.IsNullOrWhiteSpace(order.Customer.Phone))
    throw new BusinessException("Lütfen adınızı ve telefon numaranızı girin.");
if (order.Channel == OrderChannel.Delivery && string.IsNullOrWhiteSpace(order.Address))
    throw new BusinessException("Lütfen teslimat adresinizi girin.");
if (order.Channel == OrderChannel.Pickup)
{
    if (order.PickupTime == null) throw new BusinessException("Lütfen teslim alma saatini seçin.");
    if (order.PickupTime < _time.GetLocalNow().DateTime) throw new BusinessException("Teslim alma saati geçmişte olamaz.");
}
```
- `return;` sadece **bu halkanın** `Check`'inden çıkar. `Handle` devam eder ve sıradaki halkayı yine çağırır. Zincir kırılmaz, sadece bu halkanın geri kalan kontrolleri atlanır.
- `TableId == null` kontrolü, `PlaceOrderAsync`'te QR bulunamadığında bilerek bırakılan null'ı yakalıyor.
- `TimeProvider` sayesinde "geçmiş saat" kuralı test edilebilir: testte saat 12:00'ye sabitlenir, 11:59 reddedilir, 12:01 kabul edilir. `DateTime.Now` ile bu testi yazmak mümkün değildi.

### Halka 4: OrderAmountHandler
```csharp
public OrderAmountHandler(IOptions<PricingOptions> options) { _options = options.Value; }
protected override void Check(Order order)
{
    if (order.Channel == OrderChannel.Delivery && order.Subtotal < _options.MinDeliveryAmount)
        throw new BusinessException("Teslimat için en az sipariş tutarı " + _options.MinDeliveryAmount.ToString("0") + " ₺.");
}
```
Halka bağımlılığını constructor'dan alıyor ve DI veriyor. `OrderService`'in bu halkanın `PricingOptions`'a ihtiyaç duyduğunu bilmesine gerek yok.

### Test sonuçları (bugün)
| Senaryo | Duran halka | Mesaj |
|---|---|---|
| Boş sepet | 1 ProductHandler | Sepetiniz boş. |
| 95 ₺'lik teslimat | 4 OrderAmountHandler | Teslimat için en az sipariş tutarı 100 ₺. |
| Adressiz teslimat | 3 ChannelInfoHandler | Lütfen teslimat adresinizi girin. |
| 2020 tarihli gel-al | 3 ChannelInfoHandler | Teslim alma saati geçmişte olamaz. |
| Sahte QR | 3 ChannelInfoHandler | Lütfen masanızdaki QR kodu okutun. |

### Neden exception? Neden hata listesi değil?
Bu tasarım **ilk hatada durur** (fail-fast). Kullanıcı aynı anda adres de saat de eksikse önce birini görür, düzeltir, sonra diğerini görür. Alternatif: her halka bir `List<string> errors`'a ekler, sonunda hepsi birden gösterilir. Exception ile yazmak daha basit; liste yöntemi kullanıcı dostu.

### Sıra neden bu?
1. Ürün yoksa (boş sepet) diğer hiçbir kontrolün anlamı yok.
2. Adet, ürün kontrolünden sonra.
3. Kanal bilgisi.
4. Tutar en sonda, çünkü `Subtotal` hesaplanmış olmalı. Zincir zaten `ApplyPricesAsync`'ten sonra çalışıyor.

Sırayı değiştirmek için `BusinessServiceRegistration`'daki kayıt satırlarının sırasını değiştirmek yeterli.

### Genişletme alıştırması
"Gece 23:00 ile 07:00 arası teslimat yok" kuralı için:
```csharp
public class OpeningHoursHandler : OrderValidationHandler
{
    private readonly TimeProvider _time;
    public OpeningHoursHandler(TimeProvider time) { _time = time; }

    protected override void Check(Order order)
    {
        int hour = _time.GetLocalNow().Hour;
        if (order.Channel == OrderChannel.Delivery && (hour >= 23 || hour < 7))
            throw new BusinessException("Gece teslimat yapılmıyor.");
    }
}
// BusinessServiceRegistration: istediğin sıraya bir satır eklenir.
// services.AddTransient<OrderValidationHandler, OpeningHoursHandler>();
// OrderService, OrderValidationChain ve diğer halkalar değişmez.
```

---

```csharp
    // 4) Sipariş "eklenecek" işaretlenir; OBSERVER: mutfak, kasa ve admin bildirimleri eklenir.
    await _orderRepository.AddAsync(order);
    await _events.OrderPlacedAsync(order);

    // 5) UNIT OF WORK: hepsi tek SaveChanges, tek transaction
    await _unitOfWork.SaveChangesAsync();

    return _mapper.Map<ResultOrderDto>(order);
}
```
- `AddAsync(order)` **kaydetmiyor**, sadece bütün sipariş grafiğini `Added` işaretliyor (Bölüm 02).
- Observer'lar da bildirimleri sadece işaretliyor (aşağıda). Henüz `order.Id` yok, bu yüzden bildirim `OrderId` ile değil `Order` navigation'ı ile bağlanıyor: `notification.Order = order`. EF Core `SaveChanges` sırasında önce siparişi INSERT edip Id'yi alıyor, sonra bildirimlerin `OrderId`'sini dolduruyor (relationship fix-up, Bölüm 02).
- Tek `SaveChangesAsync` sipariş, kalemler, seçenekler, ilk durum kaydı, (yeniyse) müşteri ve 3 bildirimi **tek transaction'da** yazıyor. Bu satırdan sonra `order.Id`, `detail.Id` ve `customer.Id` veritabanının verdiği değerlerle dolu.

**Önemli sıralama:** Chain kayıttan **önce** çalışıyor. Herhangi bir halka exception fırlatırsa `SaveChangesAsync`'e hiç gelinmez. Veritabanına sıfır satır yazılır, yeni müşteri de oluşmaz.

---

## 9. DESEN 5: OBSERVER (bildirimler)

### Arayüz (Observer)
```csharp
public interface IOrderObserver
{
    Task OrderPlacedAsync(Order order);
    Task StatusChangedAsync(Order order, OrderStatus previous);
}
```
İki olay var. `previous` parametresi, "Alındı → Hazırlanıyor" mesajını yazabilmek için gerekli; `order.Status` artık yeni değeri taşıyor.

### Yayıncı (Subject)
```csharp
public class OrderEventPublisher : IOrderEventPublisher
{
    private readonly IEnumerable<IOrderObserver> _observers;
    private readonly ILogger<OrderEventPublisher> _logger;

    public OrderEventPublisher(IEnumerable<IOrderObserver> observers, ILogger<OrderEventPublisher> logger)
    { _observers = observers; _logger = logger; }

    public async Task OrderPlacedAsync(Order order)
    {
        foreach (IOrderObserver observer in _observers)
        {
            try { await observer.OrderPlacedAsync(order); }
            catch (Exception ex) { _logger.LogError(ex, "{OrderNo} siparişi için bildirim yazılamadı", order.OrderNo); }
        }
    }
    // StatusChangedAsync aynı kalıp
}
```
- **Abone listesi DI'dan geliyor.** Klasik GoF'ta `Attach(observer)` / `Detach(observer)` metotları olur ve abonelik çalışma anında yapılır. Burada abonelik **uygulama açılışında**, `BusinessServiceRegistration`'da sabitleniyor. Web uygulamalarında yaygın olan yöntem bu.
- `OrderService` yayıncıyı `IOrderEventPublisher` arayüzüyle istiyor. Testte sahte bir yayıncı verip "sipariş verilince `OrderPlacedAsync` çağrıldı mı?" diye kontrol edilebilir.
- **Hata izolasyonu:** Bir observer'ın kodu exception fırlatırsa (ör. mesaj hazırlarken bir hata) loglanır ve diğer observer'lar yine çalışır.
- `{OrderNo}` structured logging: log sistemi bunu ayrı bir alan olarak saklar ve aranabilir yapar. String birleştirme ile yazılmamış.
- `await` ile sırayla çalışıyorlar (paralel değil). Aynı DbContext'i paylaştıkları için paralel çalıştırmak **hata olurdu**, çünkü DbContext thread-safe değil.

> **Eskiden bir tuzak vardı:** Her observer kendi `SaveChangesAsync`'ini çağırıyordu. Biri başarısız olursa o `Notification` change tracker'da `Added` durumunda kalıyor, sıradaki observer'ın `SaveChangesAsync`'i onu **tekrar** yazmaya çalışıp o da başarısız oluyordu. Yani `try/catch` veritabanı hatalarında işe yaramıyordu. Artık observer'lar veritabanına hiç gitmiyor, sadece işaretliyor; veritabanı hatası ancak servisteki tek `SaveChangesAsync`'te olabilir ve o zaman **sipariş de kaydedilmez** (ya hepsi ya hiçbiri).

### Somut gözlemciler
```csharp
public class KitchenObserver : IOrderObserver
{
    public async Task OrderPlacedAsync(Order order)
        => await SendAsync(order, $"Yeni sipariş {order.OrderNo}: hazırlanacak {order.Details.Sum(d => d.Quantity)} ürün.");
    public async Task StatusChangedAsync(Order order, OrderStatus previous)
    {
        if (order.Status == OrderStatus.Cancelled)
            await SendAsync(order, $"{order.OrderNo} siparişi iptal edildi. Hazırlamayı durdurun.");
    }
    private async Task SendAsync(Order order, string message)
    {
        Notification notification = new Notification();
        notification.Order = order;                          // Id yeni siparişte henüz yok; EF kayıtta bağlar
        notification.Target = NotificationTarget.Kitchen;
        notification.Message = message;
        notification.CreatedAt = _time.GetLocalNow().DateTime;
        await _notificationRepository.AddAsync(notification);   // sadece işaretle; servis kaydedecek
    }
}
```
| Gözlemci | Yeni sipariş | Durum değişikliği |
|---|---|---|
| `KitchenObserver` | "hazırlanacak 4 ürün" (adetlerin toplamı) | Sadece İptal: "Hazırlamayı durdurun." |
| `CashierObserver` | `$"... Toplam: ₺{order.Total.ToString("N2", Turkish)}."` | Sadece Teslim Edildi: "siparişi teslim edildi." |
| `AdminObserver` | `$"Yeni {order.Channel.ToText()} siparişi: ..."` | Her değişiklik: `$"{previous.ToText()} → {order.Status.ToText()}"` |

- `$"..."` string interpolation; `{order.Total:0.00}` iki ondalık hane formatı.
- Her gözlemci **kendi** filtresini uyguluyor: hangi olayla ilgilendiğine kendisi karar veriyor. Yayıncı herkese her şeyi gönderiyor.
- `SendAsync` metodu üç sınıfta neredeyse aynı (sadece `Target` farklı). Kod tekrarı var; bir taban sınıf (`NotificationObserverBase`) ile tekilleştirilebilirdi.
- "Bildirim" gerçek bir SMS veya e-posta değil, `Notifications` tablosuna bir satır. Case buna izin veriyor: *"gerçek harici servis yerine açıkça belirtilmiş simülasyonlar."*

Test: CF-2936596E için 3 bildirim (placed), sonra 3 durum değişikliği → Admin 3 + Cashier 1 (Delivered) = toplam **7** bildirim. Bugünkü sorguda `Notif = 7` gördük ✔.

### Yeni bir gözlemci eklemek (Open/Closed'un kanıtı)
```csharp
public class SmsObserver : IOrderObserver
{
    private readonly ILogger<SmsObserver> _logger;
    public SmsObserver(ILogger<SmsObserver> logger) { _logger = logger; }
    public Task OrderPlacedAsync(Order order)
    {
        if (order.Customer != null) _logger.LogInformation("SMS to {Phone}: order {No} received", order.Customer.Phone, order.OrderNo);
        return Task.CompletedTask;
    }
    public Task StatusChangedAsync(Order order, OrderStatus previous) => Task.CompletedTask;
}
// BusinessServiceRegistration: services.AddScoped<IOrderObserver, SmsObserver>();
```
`OrderService`, `OrderEventPublisher` ve diğer gözlemcilere **dokunulmadı**.

### Transaction ve bildirimler
Bildirimler siparişle **aynı transaction'da** yazılıyor (Unit of Work). Eskiden sipariş önce kaydediliyor, bildirimler sonra ayrı `SaveChanges` çağrılarıyla yazılıyordu; elektrik tam o anda kesilirse sipariş kaydedilmiş ama bildirimi yazılmamış olabiliyordu. Artık ikisi birlikte yazılıyor ya da hiçbiri yazılmıyor.

Bunun bir bedeli var: bildirim tablosuna yazılamazsa (ör. bir kolon kuralı ihlal edilirse) **sipariş de kaydedilmez**. Burada bildirim sadece bir tablo satırı olduğu için bu kabul edilebilir. Bildirim gerçek bir SMS veya e-posta olsaydı, dış servisi transaction içinde çağırmak yanlış olurdu (SMS gönderilip sonra sipariş geri alınabilir). Gerçek sistemlerde bunun çözümü **Outbox pattern**'dir: olay, siparişle aynı transaction içinde bir "outbox" tablosuna yazılır, sonra ayrı bir işlem tarafından gönderilir. Bu projedeki `Notifications` tablosu aslında basit bir outbox gibi davranıyor.

---

## 10. `ChangeStatusAsync` ve DESEN 6: STATE

```csharp
public async Task ChangeStatusAsync(int id, OrderStatus target, string? note)
{
    var order = await _orderRepository.GetWithDetailsAsync(id);
    if (order == null) throw new BusinessException("Sipariş bulunamadı.");

    // STATE: şu anki durum, hedef duruma geçişe izin veriyor mu?
    OrderState state = OrderState.For(order.Status);
    if (!state.CanMoveTo(target))
        throw new BusinessException("Sipariş \"" + order.Status.ToText() + "\" durumundan \"" + target.ToText() + "\" durumuna geçemez.");

    OrderStatus previous = order.Status;
    order.Status = target;

    OrderStatusHistory history = new OrderStatusHistory();
    history.FromStatus = previous;
    history.ToStatus = target;
    history.ChangedAt = _time.GetLocalNow().DateTime;
    if (!string.IsNullOrWhiteSpace(note)) history.Note = note.Trim();
    order.StatusHistories.Add(history);

    await _events.StatusChangedAsync(order, previous);   // OBSERVER: bildirimleri işaretle
    await _unitOfWork.SaveChangesAsync();                // durum + geçmiş + bildirimler tek kayıt
}
```
Adımlar:
1. Siparişi ilişkileriyle birlikte yükle (Observer mesajları `OrderNo`'ya ihtiyaç duyuyor, `Details` de yükleniyor).
2. **State'e sor:** bu geçiş izinli mi?
3. `previous`'u sakla: değişiklikten **önce** alınmalı, yoksa kaybolur.
4. Geçmişe yeni bir satır ekle (`Id = 0` → yeni).
5. Gözlemcilere haber ver: bildirimler işaretlenir.
6. Kaydet: tek `SaveChangesAsync`. Sipariş veritabanından **izlenerek** (tracking) okundu; EF Core değişen `Status` kolonunu, yeni history satırını (`Id = 0` → `Added`) ve yeni bildirimleri kendisi buluyor. `Update(order)` çağrılmıyor.

**Eskiden** burada `UpdateAsync(order)` → `_context.Update(order)` çağrılıyordu. Bu, grafikteki bütün izlenen nesneleri `Modified` işaretliyordu; Customer, Table, Details ve Options için de gereksiz UPDATE cümleleri gidiyordu (değerler aynı olduğu için zararsız, ama israf). Şimdi sadece `Orders.Status` UPDATE ediliyor. Ayrıca bildirimler de aynı transaction'a girdi.

### State sınıfları
```csharp
public abstract class OrderState
{
    public abstract List<OrderStatus> GetAllowedNext();

    public bool CanMoveTo(OrderStatus target) => GetAllowedNext().Contains(target);

    public static OrderState For(OrderStatus status)
    {
        if (status == OrderStatus.Received)  return new ReceivedState();
        if (status == OrderStatus.Preparing) return new PreparingState();
        if (status == OrderStatus.Ready)     return new ReadyState();
        if (status == OrderStatus.Delivered) return new DeliveredState();
        return new CancelledState();
    }
}

public class ReceivedState  : OrderState { public override List<OrderStatus> GetAllowedNext() => new() { OrderStatus.Preparing, OrderStatus.Cancelled }; }
public class PreparingState : OrderState { public override List<OrderStatus> GetAllowedNext() => new() { OrderStatus.Ready, OrderStatus.Cancelled }; }
public class ReadyState     : OrderState { public override List<OrderStatus> GetAllowedNext() => new() { OrderStatus.Delivered }; }
public class DeliveredState : OrderState { public override List<OrderStatus> GetAllowedNext() => new(); }
public class CancelledState : OrderState { public override List<OrderStatus> GetAllowedNext() => new(); }
```
(Kodda her biri açık `return new List<OrderStatus> { ... };` biçiminde yazılı; burada kısaltıldı.)

```text
             ┌──────────┐   ┌───────────┐   ┌───────┐   ┌───────────┐
  (yeni) ──► │ Received │──►│ Preparing │──►│ Ready │──►│ Delivered │  (son)
             └────┬─────┘   └─────┬─────┘   └───────┘   └───────────┘
                  │               │
                  ▼               ▼
             ┌───────────────────────┐
             │       Cancelled       │  (son)
             └───────────────────────┘
```

### Aynı kuralın iki tüketicisi
1. **Servis** (`ChangeStatusAsync`): izinsiz geçişi reddediyor. Bu gerçek güvenlik: biri formu elle gönderse bile engellenir.
2. **Arayüz:** `OrderQueryService.GetByIdAsync` izinli durumları DTO'ya koyuyor (`dto.AllowedNextStatuses = OrderState.For(order.Status).GetAllowedNext();`); admin sipariş detayı sadece bunların düğmesini çiziyor. Bu kullanıcı deneyimi. Controller State sınıfını hiç tanımıyor.

Bugün UI'dan denedik: Preparing durumundaki bir siparişe `target=Delivered` gönderdik (normalde düğmesi yok) → 'Sipariş "Hazırlanıyor" durumundan "Teslim Edildi" durumuna geçemez.'

### Bu uygulama klasik State'ten nasıl ayrılıyor? (sınav sorusu)
Klasik GoF State:
```csharp
class Order { OrderState _state; public void Cancel() => _state.Cancel(this); }   // davranış duruma DELEGE edilir
class ReceivedState  : OrderState { public override void Cancel(Order o) { o.SetState(new CancelledState()); /* mutfağa haber... */ } }
class DeliveredState : OrderState { public override void Cancel(Order o) => throw new InvalidOperationException(); }
```
Yani nesnenin **davranışı** durumuna göre değişir ve durum geçişini durum nesnesi kendisi yapar.

Bu projede:
- Durum sınıfları sadece **"nereye gidebilirim?"** sorusunu cevaplıyor (geçiş tablosu).
- Geçişi `OrderService` yapıyor.
- Entity (`Order`) durum nesnesi tutmuyor; durum bir enum. Gerektiğinde `OrderState.For(...)` ile nesneye çevriliyor (Simple Factory).

Bu, State deseninin **hafif, geçiş odaklı** bir uygulaması. Case'in istediği de buydu: *"izinli geçişler"*. Genişletmek istersen `OrderState`'e `virtual Task OnEnterAsync(Order order)` eklenir. Örneğin `ReadyState.OnEnterAsync` "müşteriye siparişin hazır olduğunu bildir" der. Böylece davranış da duruma taşınmış olur.

Neden enum + `For()`? EF Core bir abstract sınıfı kolon olarak saklayamaz; enum saklanabilir. Entity'yi saf tutmak için durum nesnesi kalıcı alan değil, hesaplanan bir şey.

### Kendini kontrol: "Ready siparişi de iptal edilebilsin" dersen ne değişir?
Sadece bir satır:
```csharp
public class ReadyState : OrderState { ... => new List<OrderStatus> { OrderStatus.Delivered, OrderStatus.Cancelled }; }
```
Servis, controller ve view değişmez: admin ekranında "Cancel order" düğmesi kendiliğinden çıkar. Desenin kazancı tam olarak bu.

---

## 11. Desenlerin birbirleriyle konuşması

```text
CartController.Checkout
      │ CreateOrderDto + Session sepeti + Session QR
      ▼
CartService.CheckoutAsync → OrderService.PlaceOrderAsync
      │
      ├─ AutoMapper ............ CreateOrderDto → Order(form)
      ├─ CustomerRepository .... telefon → var olan / yeni müşteri
      ├─ TableRepository ....... QR → aktif masa (yoksa null)
      │
      ├─ [FACTORY METHOD] _creators[channel].Create(form, table)          ← IEnumerable<OrderCreator> (DI)
      │        └─ CreateForChannel (kanala özel alanlar) + ortak alanlar + ilk geçmiş kaydı
      │
      ├─ IOrderPricingService.ApplyPricesAsync
      │     ├─ [DECORATOR] her kalem: BaseProductPrice ⟵ OptionPriceDecorator × n
      │     └─ [STRATEGY]  _feeStrategies[channel].Calculate(subtotal)      ← IEnumerable<IDeliveryFeeStrategy> (DI)
      │
      ├─ [CHAIN] IOrderValidationChain: Product → Quantity → ChannelInfo → OrderAmount   ← kayıt sırası (DI)
      │          (hata = BusinessException, kayıt yok)
      │
      ├─ OrderRepository.AddAsync(order)                    (sadece işaretler)
      ├─ [OBSERVER] IOrderEventPublisher → Kitchen, Cashier, Admin → NotificationRepository.AddAsync
      │
      └─ IUnitOfWork.SaveChangesAsync   (Customer?, Order, Details, Options, History, Notifications: TEK transaction)

OrdersController.ChangeStatus
      ▼
OrderService.ChangeStatusAsync
      ├─ [STATE] OrderState.For(status).CanMoveTo(target)
      ├─ StatusHistories.Add(...)
      ├─ [OBSERVER] StatusChangedAsync   (bildirimleri işaretler)
      └─ IUnitOfWork.SaveChangesAsync   (Status + yeni geçmiş + bildirimler)
```

## 12. Uygulanmayan 4 desen (bilmen gereken)
Case 10 desenden 6'sını istiyor. Projede uygulanmayanlar ve bu projede nereye oturabilecekleri:

| Desen | Bu projede nereye oturur? |
|---|---|
| **Builder** | `OrderCreator` + `ApplyPricesAsync` yerine `new OrderBuilder().ForChannel(...).WithCustomer(...).AddItem(...).WithNote(...).Build()` |
| **Adapter** | `INotificationSender` arayüzü + `TwilioSmsAdapter`, `MockEmailAdapter`. Farklı dış servis API'lerini ortak bir arayüze uyarlamak. Observer'lar bunu kullanırdı. |
| **Specification** | `ProductRepository.Active()` ve `OrderRepository.Filter(...)`, `ISpecification<Order>` sınıflarına dönüşür: `StatusSpec`, `ChannelSpec`, `DateRangeSpec`; `And()`/`Or()` ile birleştirilir. |
| **Command** | Admin eylemleri sınıflara ayrılır: `ChangeStatusCommand`, `CancelOrderCommand` (`Execute()`), isteğe bağlı olarak `Undo()` ve komut geçmişi. |

---

## Kendini sına
1. Sepet sayfasında gösterilen tutar ile kaydedilen tutarın asla farklı olamayacağını hangi tasarım kararı garanti ediyor?
2. `detail.Product = product` satırı, ürün başka bir DbContext'ten gelseydi neden sorun çıkarırdı?
3. `SetNext` `this` döndürseydi zincir nasıl olurdu? Hangi halkalar çalışmazdı?
4. `ChannelInfoHandler`'daki `return;` zinciri durdurur mu?
5. Masa siparişinde neden `CustomerId` NULL? `Customer` nesnesi oluşturulduğu halde neden kaydedilmiyor?
6. Observer'lar neden `SaveChanges` çağırmıyor? Eskiden çağırdıklarında bir observer'ın hatası neden sıradakini de bozabiliyordu?
7. "Ready → Cancelled" geçişine izin vermek kaç dosyayı değiştirir?
8. Bu projedeki State uygulamasını klasik GoF State'e çevirmek için ne yapardın?
9. Yeni bir "Kurye-Express" kanalı eklemek için hangi dosyalara yeni sınıf, hangilerine bir satır eklemen gerekir? `OrderService` değişir mi?
10. Doğrulama halkaları neden Transient, ücret stratejileri neden Singleton kaydedilmiş?
