# 05 · OrderService ve 6 tasarım deseni (projenin kalbi)

> Bu bölüm en uzun ve en önemli bölüm. `OrderService`'i baştan sona satır satır okuyoruz ve her desene geldiğimizde o desenin bütün sınıflarını açıyoruz. Örnek olarak bugün test ettiğimiz gerçek bir siparişi izliyoruz:
>
> **CF-2936596E** · Teslimat · Ayşe Yılmaz · 2 × Vanilyalı Latte (Büyük + Yulaf Sütü + Shot), 1 × Vanilyalı Latte (Küçük + Normal Süt + Vanilya Şurubu), 1 × Tereyağlı Kruvasan

---

## 1. OrderService'in bağımlılıkları

```csharp
public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IProductOptionRepository _productOptionRepository;
    private readonly ITableRepository _tableRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly PricingOptions _pricing;          // Strategy ve Chain için rakamlar
    private readonly OrderEventPublisher _events;      // Observer yayıncısı
    private readonly IMapper _mapper;
```
8 bağımlılık. Bu sayı bir "kod kokusu" sayılabilir: sınıf çok iş yapıyor olabilir (Single Responsibility). Burada bilinçli bir tercih var: desenlerin **sırayla çalıştığı tek yer** burası, öğrenirken akışı tek dosyada görmek kolay. Daha büyük bir projede fiyat hesabı ayrı bir `OrderPricingService`'e, doğrulama ayrı bir `OrderValidator`'a çıkarılırdı.

`IOrderService`'in sunduğu sözleşme:
```csharp
Task<List<ResultOrderDto>> GetAllAsync();
Task<ResultOrderDto?> GetByIdAsync(int id);
Task<ResultOrderDto?> GetByOrderNoAsync(string orderNo);
Task<int> CountByStatusAsync(OrderStatus status);
Task<ResultOrderDto> CalculateAsync(OrderChannel channel, List<CartItemDto> items);   // kaydetmez
Task<ResultOrderDto> PlaceOrderAsync(CreateOrderDto dto, List<CartItemDto> items, string? qrToken);
Task ChangeStatusAsync(int id, OrderStatus target, string? note);
```

---

## 2. Okuma metotları

```csharp
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
```
- `Customer` ve `Table` → listede "kimin siparişi" sütunu (`CustomerFullName`, `TableNumber` flattening ile).
- `Details` → dashboard'daki "en çok satanlar" hesabı. `Details.Options` yüklenmiyor; listede seçenek gösterilmediği için gerekmiyor.
- `OrderByDescending(o => o.Id)` → en yeni sipariş en üstte.
- **Not:** Bu metot **bütün** siparişleri RAM'e çekiyor. Admin sipariş listesi filtrelemeyi ve sayfalamayı RAM'de yapıyor (Bölüm 07). 18 siparişte sorun yok; 100.000 siparişte filtre ve sayfalama SQL'e taşınmalı (`Skip/Take` IQueryable üzerinde).

```csharp
public async Task<ResultOrderDto?> GetByIdAsync(int id)
{
    var order = await OrdersWithDetails().FirstOrDefaultAsync(o => o.Id == id);
    if (order == null) return null;
    ResultOrderDto dto = _mapper.Map<ResultOrderDto>(order);
    dto.AllowedNextStatuses = OrderState.For(order.Status).GetAllowedNext();   // STATE: izinli sonraki durumlar
    return dto;
}

private IQueryable<Order> OrdersWithDetails()
{
    return _orderRepository.GetQueryable()
        .Include(o => o.Customer)
        .Include(o => o.Table)
        .Include(o => o.Details).ThenInclude(d => d.Options)
        .Include(o => o.StatusHistories);
}
```
`ThenInclude`: iki seviye derin yükleme (Order → Details → Options). `OrdersWithDetails()` üç yerde kullanılıyor: detay, onay sayfası ve durum değiştirme. Sorgu tekrar yazılmasın diye ayrı metoda alınmış (DRY: Don't Repeat Yourself).

`GetByOrderNoAsync` aynı mantıkla çalışıyor. `CartController.Confirmation` bunu `orderNo.ToUpper()` ile çağırıyor.

```csharp
public async Task<int> CountByStatusAsync(OrderStatus status)
    => await _orderRepository.CountAsync(o => o.Status == status);
```
Sidebar'daki "Siparişler 1" rozeti: `Received` sayısı.

---

## 3. Fiyat hesabı: `CalculateAsync` ve `CalculateOrderAsync`

### Sepet için: kaydetmeden hesapla
```csharp
public async Task<ResultOrderDto> CalculateAsync(OrderChannel channel, List<CartItemDto> items)
{
    Order order = new Order();
    order.Channel = channel;
    await CalculateOrderAsync(order, items);
    return _mapper.Map<ResultOrderDto>(order);
}
```
Sepet sayfası ve sağdan açılan sepet paneli bu metodu çağırıyor. Bellekte geçici bir `Order` oluşuyor, hesaplanıyor, DTO'ya çevrilip ekrana gidiyor. **Hiçbir zaman veritabanına yazılmıyor** (repository'ye verilmiyor).

**Asıl tasarım fikri:** sepet ekranı ve gerçek sipariş **aynı** `CalculateOrderAsync`'i kullanıyor. Kullanıcının sepette gördüğü tutar ile kaydedilen tutar asla farklı olamaz, çünkü iki ayrı hesap kodu yok.

### CalculateOrderAsync: adım adım

```csharp
private async Task CalculateOrderAsync(Order order, List<CartItemDto> items)
{
    // 1) Sepetteki ürünleri ve bütün seçenekleri veritabanından al.
    List<int> productIds = new List<int>();
    foreach (CartItemDto item in items) productIds.Add(item.ProductId);

    var products = await _productRepository.GetQueryable()
        .Include(p => p.Category)
        .Where(p => productIds.Contains(p.Id))
        .ToListAsync();

    var allOptions = await _productOptionRepository.GetAllAsync();
```
- `productIds.Contains(p.Id)` → SQL `WHERE Id IN (2, 2, 14)`. Sepetteki bütün ürünler **tek sorguda** geliyor. Her sepet satırı için ayrı sorgu atılsaydı bu "N+1 problemi" olurdu.
- `.Include(p => p.Category)` iki iş görüyor: (a) Chain'deki `ProductHandler` `detail.Product.Category.IsActive`'e bakıyor, (b) sepette kategori adı gösteriliyor.
- **Dikkat:** Burada `IsActive` filtresi **yok**. Pasif yapılmış bir ürün sepette kalıyor ve hesaplanıyor; siparişi engelleyen şey Chain'deki `ProductHandler` ("... is no longer available."). Bu bilinçli: kullanıcı ürünün neden gittiğini mesajla görüyor.
- `allOptions`: 9 seçeneğin hepsi tek sorguda geliyor (küçük bir tablo).

CF-2936596E için: `productIds = [2, 2, 14]` → 2 ürün geldi (Vanilyalı Latte, Tereyağlı Kruvasan).

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
`product` bu istekte **aynı DbContext** tarafından yüklendi, dolayısıyla change tracker onu `Unchanged` olarak tanıyor. Sipariş kaydedilirken `AddAsync(order)` bütün grafiği gezer: yeni nesneleri `Added` yapar, zaten izlenen `product`'a ise dokunmaz. Eğer `product` başka bir DbContext'ten gelmiş (veya `AsNoTracking` ile okunmuş) olsaydı, EF onu yeni bir ürün sanıp **INSERT etmeye çalışırdı** ve çift kayıt ya da hata olurdu. Bölüm 01'de "repository'ler aynı DbContext'i paylaşıyor" demiştik; bu kod o sayede doğru çalışıyor.

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
    if (order.Details.Count == 0)
        order.DeliveryFee = 0;
    else
    {
        IDeliveryFeeStrategy feeStrategy = GetFeeStrategy(order.Channel);
        order.DeliveryFee = feeStrategy.Calculate(order.Subtotal);
    }
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
    decimal Calculate(decimal orderAmount);
}

// Pricing/Fees/DeliveryFeeStrategy.cs
public class DeliveryFeeStrategy : IDeliveryFeeStrategy
{
    private readonly PricingOptions _options;
    public DeliveryFeeStrategy(PricingOptions options) { _options = options; }
    public decimal Calculate(decimal orderAmount)
    {
        if (orderAmount >= _options.FreeDeliveryFrom) return 0;   // 600 ve üzeri ücretsiz
        return _options.DeliveryFee;                              // 30
    }
}

// Pricing/Fees/PickupFeeStrategy.cs
public class PickupFeeStrategy : IDeliveryFeeStrategy { public decimal Calculate(decimal orderAmount) { return 0; } }

// Pricing/Fees/TableFeeStrategy.cs
public class TableFeeStrategy : IDeliveryFeeStrategy { public decimal Calculate(decimal orderAmount) { return 0; } }

// Pricing/PricingOptions.cs
public class PricingOptions
{
    public decimal DeliveryFee { get; set; } = 30;
    public decimal FreeDeliveryFrom { get; set; } = 600;
    public decimal MinDeliveryAmount { get; set; } = 100;   // Chain'deki OrderAmountHandler kullanır
}
```

### Seçim (Context)
```csharp
private IDeliveryFeeStrategy GetFeeStrategy(OrderChannel channel)
{
    if (channel == OrderChannel.Delivery) return new DeliveryFeeStrategy(_pricing);
    if (channel == OrderChannel.Pickup)   return new PickupFeeStrategy();
    return new TableFeeStrategy();
}
```
| GoF rolü | Bu projede |
|---|---|
| Strategy | `IDeliveryFeeStrategy` |
| ConcreteStrategy | `DeliveryFeeStrategy`, `PickupFeeStrategy`, `TableFeeStrategy` |
| Context | `OrderService.CalculateOrderAsync` (stratejiyi kullanan) |

### Test sonuçları
| Sipariş | Ara toplam | Strateji | Ücret |
|---|---|---|---|
| CF-28ED1FE2 (Zeynep, teslimat) | 275 | Delivery, 275 < 600 | **30** → toplam 305 |
| CF-2936596E (Ayşe, teslimat) | 715 | Delivery, 715 ≥ 600 | **0** |
| CF-91F40FE4 (Mehmet, gel-al) | 705 | Pickup | 0 |
| CF-92B1EF1D (Masa 7) | 885 | Table | 0 |

### Eleştiri ve iyileştirme (bilmen gereken)
`GetFeeStrategy` bir `if` zinciri. Yeni bir kanal (örneğin "Kurye-Express") eklenirse bu metoda dokunmak gerekir (Open/Closed ihlali). DI ile çözüm:
```csharp
public interface IDeliveryFeeStrategy { OrderChannel Channel { get; } decimal Calculate(decimal amount); }
// Program.cs: her strateji AddScoped<IDeliveryFeeStrategy, X>() ile kaydedilir
// OrderService: IEnumerable<IDeliveryFeeStrategy> alır ve
//   _strategies.Single(s => s.Channel == order.Channel)
```
Bu şekilde yeni kanal = yeni sınıf + Program.cs'e bir satır. (Observer'da tam olarak bu teknik kullanılıyor.)

İsim notu: `PickupFeeStrategy` ve `TableFeeStrategy`'nin ikisi de 0 döndürüyor, ve arayüzün adı `IDeliveryFeeStrategy`. Aslında "kanal ücreti stratejisi" demek daha doğru olurdu. Ayrı sınıflar olmaları ise doğru: ileride "masa servisi %10" kuralı geldiğinde sadece `TableFeeStrategy` değişir.

---

## 6. `PlaceOrderAsync`: sipariş verme, baştan sona

```csharp
public async Task<ResultOrderDto> PlaceOrderAsync(CreateOrderDto dto, List<CartItemDto> items, string? qrToken)
{
    // DTO → Entity
    Order form = _mapper.Map<Order>(dto);
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

```csharp
    // Müşteri: aynı telefonla daha önce sipariş veren müşteri tekrar oluşturulmaz.
    Customer customer = new Customer();
    customer.FullName = (dto.FullName ?? "").Trim();
    customer.Phone = (dto.Phone ?? "").Trim();
    if (customer.Phone != "")
    {
        string phone = customer.Phone;
        var existing = await _customerRepository.GetFirstAsync(c => c.Phone == phone);
        if (existing != null) customer = existing;
    }
    form.Customer = customer;
```
İnce noktalar:
- **Telefon = müşteri kimliği.** Ayşe ikinci kez sipariş verdiğinde (`CF-ADF38C79`) yeni müşteri oluşmadı, `CustomerId = 1` tekrar kullanıldı. Bugünkü sorguda 4 sipariş için 3 müşteri görmemizin sebebi bu.
- Var olan müşteri bulunursa formda yazılan **ad yok sayılıyor** (`customer = existing`). Aynı telefonla farklı bir isim yazılsa bile kayıttaki eski isim kalır.
- `string phone = customer.Phone;` neden ayrı bir değişkene alınmış? Lambda (`c => c.Phone == phone`) içinde `customer.Phone` kullanılsaydı da çalışırdı. Ayrı değişken sorgunun neye bağlı olduğunu netleştiriyor; EF Core yerel değişkeni SQL parametresine çeviriyor.
- Masa siparişinde de bir `Customer` nesnesi oluşuyor (boş ad ve telefonla) ve `form.Customer`'a atanıyor. Ama `TableOrderCreator` müşteriyi **kopyalamıyor**, dolayısıyla bu nesne hiçbir zaman kaydedilmiyor. Masa siparişlerinde `CustomerId = NULL` olmasının sebebi bu.

```csharp
    // 1) FACTORY METHOD
    OrderCreator creator = GetCreator(form.Channel);
    Order order = creator.Create(form, table);
```

---

## 7. DESEN 3: FACTORY METHOD (kanala göre sipariş nesnesi)

### Soyut yaratıcı
```csharp
public abstract class OrderCreator
{
    public Order Create(Order form, Table? table)
    {
        // 1) Türe özgü kısmı alt sınıf doldurur.
        Order order = CreateForChannel(form, table);

        // 2) Ortak kısım
        order.OrderNo = CreateOrderNo();
        order.Status = OrderStatus.Received;
        order.Note = form.Note;
        order.CreatedAt = DateTime.Now;

        // 3) İlk durum kaydı: "Alındı"
        OrderStatusHistory history = new OrderStatusHistory();
        history.ToStatus = OrderStatus.Received;      // FromStatus = null
        history.ChangedAt = order.CreatedAt;
        order.StatusHistories.Add(history);

        return order;
    }

    protected abstract Order CreateForChannel(Order form, Table? table);   // ← FACTORY METHOD

    private string CreateOrderNo()
    {
        string guid = Guid.NewGuid().ToString("N");   // 32 hex karakter, tiresiz
        return "CF-" + guid.Substring(0, 8).ToUpper(); // CF-2936596E
    }
}
```
- `protected abstract`: dışarıdan çağrılamaz, alt sınıf yazmak **zorunda**.
- `Create` metodu `virtual` değil, alt sınıf onu ezemez. Ortak adımların sırası sabit kalır. Bu **Template Method** desenidir: iskelet üst sınıfta, değişen adım alt sınıfta.
- Sipariş numarası: GUID'in ilk 8 hex karakteri → 16⁸ ≈ 4.3 milyar olasılık. Çakışma ihtimali çok düşük; olursa `OrderNo` üzerindeki unique index INSERT'i reddeder ve exception olur (yeniden deneme yok). Rastgele olmasının bir faydası da var: sıralı bir numara (`CF-0001`) olsaydı biri `/cart/confirmation/CF-0002` yazıp başkasının siparişini görebilirdi.
- `DateTime.Now`: sunucunun yerel saati. Birden fazla saat dilimi olsaydı `DateTime.UtcNow` tercih edilirdi.

### Somut yaratıcılar
```csharp
public class DeliveryOrderCreator : OrderCreator
{
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

### Seçici (Simple Factory)
```csharp
private OrderCreator GetCreator(OrderChannel channel)
{
    if (channel == OrderChannel.Delivery) return new DeliveryOrderCreator();
    if (channel == OrderChannel.Pickup)   return new PickupOrderCreator();
    return new TableOrderCreator();
}
```

### GoF terimleri ve sınavda söylenecek cümle
| GoF rolü | Bu projede |
|---|---|
| Creator | `OrderCreator` |
| ConcreteCreator | `DeliveryOrderCreator`, `PickupOrderCreator`, `TableOrderCreator` |
| factoryMethod() | `CreateForChannel` |
| Product | `Order` |
| Creator'ın factory method'u kullanan işlemi | `Create` (Template Method) |

> *"Factory Method, `CreateForChannel`. `OrderCreator.Create` ortak adımları sabitleyip türe özgü üretimi alt sınıfa bırakıyor, yani Template Method ile birlikte çalışıyor. Hangi alt sınıfın kullanılacağına `GetCreator` karar veriyor; bu bir Simple Factory."*

Klasik Factory Method'da "Product" da çoğu zaman bir hiyerarşidir (`DeliveryOrder : Order`). Burada ürün tipi tek (`Order`), farklı olan **nasıl doldurulduğu**. Bu da geçerli ve yaygın bir kullanım.

---

```csharp
    // 2) DECORATOR + STRATEGY
    await CalculateOrderAsync(order, items);
```
Yukarıda incelediğimiz hesap, bu sefer gerçek sipariş için çalışıyor. Fiyatlar veritabanından **yeniden** okunuyor; sepet sayfasında hesaplanan değerler hiç kullanılmıyor.

```csharp
    // 3) CHAIN OF RESPONSIBILITY
    ProductHandler chain = new ProductHandler();
    chain.SetNext(new QuantityHandler())
         .SetNext(new ChannelInfoHandler())
         .SetNext(new OrderAmountHandler(_pricing));
    chain.Handle(order);
```

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

### Zincirin kurulması, adım adım
```csharp
ProductHandler chain = new ProductHandler();          // chain → P
chain.SetNext(new QuantityHandler())                   // P._next = Q,  döner: Q
     .SetNext(new ChannelInfoHandler())                // Q._next = C,  döner: C
     .SetNext(new OrderAmountHandler(_pricing));       // C._next = A,  döner: A
// Sonuç: P → Q → C → A → null
chain.Handle(order);                                   // P.Check → Q.Check → C.Check → A.Check
```
`SetNext` `this` döndürseydi: `P._next = Q`, sonra `P._next = C` (Q'nun üstüne yazılır!), sonra `P._next = A`. Zincir **P → A** olurdu ve Q ile C hiç çalışmazdı. `return next;` satırı bu yüzden kritik.

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
- `detail.Product.Category.IsActive`: `CalculateOrderAsync`'teki `Include(p => p.Category)` olmasaydı burada `NullReferenceException` olurdu.
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
    if (order.PickupTime < DateTime.Now) throw new BusinessException("Pickup time cannot be in the past.");
}
```
- `return;` sadece **bu halkanın** `Check`'inden çıkar. `Handle` devam eder ve sıradaki halkayı yine çağırır. Zincir kırılmaz, sadece bu halkanın geri kalan kontrolleri atlanır.
- `TableId == null` kontrolü, `PlaceOrderAsync`'te QR bulunamadığında bilerek bırakılan null'ı yakalıyor.

### Halka 4: OrderAmountHandler
```csharp
public OrderAmountHandler(PricingOptions options) { _options = options; }
protected override void Check(Order order)
{
    if (order.Channel == OrderChannel.Delivery && order.Subtotal < _options.MinDeliveryAmount)
        throw new BusinessException("Teslimat için en az sipariş tutarı " + _options.MinDeliveryAmount.ToString("0") + " ₺.");
}
```
Halka bağımlılığını constructor'dan alıyor. Halkalar `new` ile oluşturulduğu için `OrderService` `_pricing`'i elle veriyor.

### Test sonuçları (bugün)
| Senaryo | Duran halka | Mesaj |
|---|---|---|
| Boş sepet | 1 ProductHandler | Your cart is empty. |
| 95 ₺'lik teslimat | 4 OrderAmountHandler | Teslimat için en az sipariş tutarı 100 ₺. |
| Adressiz teslimat | 3 ChannelInfoHandler | Lütfen teslimat adresinizi girin. |
| 2020 tarihli gel-al | 3 ChannelInfoHandler | Pickup time cannot be in the past. |
| Sahte QR | 3 ChannelInfoHandler | Lütfen masanızdaki QR kodu okutun. |

### Neden exception? Neden hata listesi değil?
Bu tasarım **ilk hatada durur** (fail-fast). Kullanıcı aynı anda adres de saat de eksikse önce birini görür, düzeltir, sonra diğerini görür. Alternatif: her halka bir `List<string> errors`'a ekler, sonunda hepsi birden gösterilir. Exception ile yazmak daha basit; liste yöntemi kullanıcı dostu.

### Sıra neden bu?
1. Ürün yoksa (boş sepet) diğer hiçbir kontrolün anlamı yok.
2. Adet, ürün kontrolünden sonra.
3. Kanal bilgisi.
4. Tutar en sonda, çünkü `Subtotal` hesaplanmış olmalı. Zincir zaten `CalculateOrderAsync`'ten sonra çalışıyor.

### Genişletme alıştırması
"Gece 23:00 ile 07:00 arası teslimat yok" kuralı için:
```csharp
public class OpeningHoursHandler : OrderValidationHandler
{
    protected override void Check(Order order)
    {
        int hour = DateTime.Now.Hour;
        if (order.Channel == OrderChannel.Delivery && (hour >= 23 || hour < 7))
            throw new BusinessException("Delivery is not available at night.");
    }
}
// OrderService: .SetNext(new OpeningHoursHandler()) eklenir. Diğer hiçbir halka değişmez.
```

---

```csharp
    // 4) Kayıt: tek transaction
    await _orderRepository.CreateWithTransactionAsync(order);
```
Bölüm 02'de anlatıldı. Bu satırdan sonra `order.Id`, `detail.Id` ve yeni müşteri ise `customer.Id` veritabanının verdiği değerlerle dolu. Observer'ların `notification.OrderId = order.Id` yazabilmesi bu yüzden mümkün.

**Önemli sıralama:** Chain kayıttan **önce** çalışıyor. Herhangi bir halka exception fırlatırsa `CreateWithTransactionAsync`'e hiç gelinmez. Veritabanına sıfır satır yazılır, yeni müşteri de oluşmaz.

```csharp
    // 5) OBSERVER
    await _events.OrderPlacedAsync(order);

    return _mapper.Map<ResultOrderDto>(order);
}
```

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
public class OrderEventPublisher
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
            catch (Exception ex) { _logger.LogError(ex, "Notification failed for order {OrderNo}", order.OrderNo); }
        }
    }
    // StatusChangedAsync aynı kalıp
}
```
- **Abone listesi DI'dan geliyor.** Klasik GoF'ta `Attach(observer)` / `Detach(observer)` metotları olur ve abonelik çalışma anında yapılır. Burada abonelik **uygulama açılışında**, `Program.cs`'te sabitleniyor. Web uygulamalarında yaygın olan yöntem bu.
- **Hata izolasyonu:** Mutfak bildirimi patlarsa kasa ve admin yine haber alır; sipariş zaten kaydedilmiş olduğu için müşteriye hata gösterilmez. Hata loglanır.
- `{OrderNo}` structured logging: log sistemi bunu ayrı bir alan olarak saklar ve aranabilir yapar. String birleştirme ile yazılmamış.
- `await` ile sırayla çalışıyorlar (paralel değil). Aynı DbContext'i paylaştıkları için paralel çalıştırmak **hata olurdu**, çünkü DbContext thread-safe değil.

> **Gizli bir tuzak:** Bir observer'ın `SaveChangesAsync`'i başarısız olursa, o `Notification` nesnesi DbContext'in change tracker'ında `Added` durumunda kalır. Sıradaki observer `SaveChangesAsync` çağırdığında EF onu **tekrar** yazmaya çalışır ve o da başarısız olur. Yani `try/catch` "biri bozulursa diğerleri çalışır" niyetini veritabanı hatalarında tam karşılamayabilir. Çözüm: hata durumunda o entity'yi tracker'dan ayırmak (`_context.Entry(n).State = Detached`) ya da her observer'a ayrı bir scope/DbContext vermek. İleri seviye bir ayrıntı, ama mülakatta çok değerli.

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
        notification.OrderId = order.Id;
        notification.Target = NotificationTarget.Kitchen;
        notification.Message = message;
        notification.CreatedAt = DateTime.Now;
        await _notificationRepository.CreateAsync(notification);
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
// Program.cs: builder.Services.AddScoped<IOrderObserver, SmsObserver>();
```
`OrderService`, `OrderEventPublisher` ve diğer gözlemcilere **dokunulmadı**.

### Transaction ve bildirimler
Bildirimler sipariş transaction'ı **commit edildikten sonra**, ayrı `SaveChanges` çağrılarıyla yazılıyor. Elektrik tam o anda kesilirse sipariş kaydedilmiş ama bildirimi yazılmamış olabilir. Gerçek sistemlerde bunun çözümü **Outbox pattern**'dir: olay, siparişle aynı transaction içinde bir "outbox" tablosuna yazılır, sonra ayrı bir işlem tarafından gönderilir.

---

## 10. `ChangeStatusAsync` ve DESEN 6: STATE

```csharp
public async Task ChangeStatusAsync(int id, OrderStatus target, string? note)
{
    var order = await OrdersWithDetails().FirstOrDefaultAsync(o => o.Id == id);
    if (order == null) throw new BusinessException("Order not found.");

    // STATE: şu anki durum, hedef duruma geçişe izin veriyor mu?
    OrderState state = OrderState.For(order.Status);
    if (!state.CanMoveTo(target))
        throw new BusinessException("Sipariş \"" + order.Status.ToText() + "\" durumundan \"" + target.ToText() + "\" durumuna geçemez.");

    OrderStatus previous = order.Status;
    order.Status = target;

    OrderStatusHistory history = new OrderStatusHistory();
    history.FromStatus = previous;
    history.ToStatus = target;
    history.ChangedAt = DateTime.Now;
    if (!string.IsNullOrWhiteSpace(note)) history.Note = note.Trim();
    order.StatusHistories.Add(history);

    await _orderRepository.UpdateAsync(order);
    await _events.StatusChangedAsync(order, previous);   // OBSERVER
}
```
Adımlar:
1. Siparişi ilişkileriyle birlikte yükle (Observer mesajları `OrderNo`'ya ihtiyaç duyuyor, `Details` de yükleniyor).
2. **State'e sor:** bu geçiş izinli mi?
3. `previous`'u sakla: değişiklikten **önce** alınmalı, yoksa kaybolur.
4. Geçmişe yeni bir satır ekle (`Id = 0` → yeni).
5. Kaydet: `UpdateAsync` → `_context.Update(order)`. Bu, grafikteki bütün izlenen nesneleri `Modified` işaretler, yeni history'yi ise `Added`. Yani Customer, Table, Details ve Options için de gereksiz UPDATE cümleleri gider (değerler aynı olduğu için zararsız, ama israf). Nesne zaten izlendiği için `Update` çağırmadan sadece `SaveChangesAsync` yeterli olurdu; o zaman EF sadece değişen `Status` kolonunu güncellerdi.
6. Gözlemcilere haber ver.

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
2. **Arayüz:** `OrderService.GetByIdAsync` izinli durumları DTO'ya koyuyor (`dto.AllowedNextStatuses = OrderState.For(order.Status).GetAllowedNext();`); admin sipariş detayı sadece bunların düğmesini çiziyor. Bu kullanıcı deneyimi. Controller State sınıfını hiç tanımıyor.

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
OrderService.PlaceOrderAsync
      │
      ├─ AutoMapper ............ CreateOrderDto → Order(form)
      ├─ TableRepository ....... QR → aktif masa (yoksa null)
      ├─ CustomerRepository .... telefon → var olan / yeni müşteri
      │
      ├─ [FACTORY METHOD] GetCreator(channel).Create(form, table)
      │        └─ CreateForChannel (kanala özel alanlar) + ortak alanlar + ilk geçmiş kaydı
      │
      ├─ CalculateOrderAsync
      │     ├─ [DECORATOR] her kalem: BaseProductPrice ⟵ OptionPriceDecorator × n
      │     └─ [STRATEGY]  GetFeeStrategy(channel).Calculate(subtotal)
      │
      ├─ [CHAIN] Product → Quantity → ChannelInfo → OrderAmount   (hata = BusinessException, kayıt yok)
      │
      ├─ OrderRepository.CreateWithTransactionAsync   (Customer?, Order, Details, Options, History)
      │
      └─ [OBSERVER] Publisher → Kitchen, Cashier, Admin → Notifications tablosu

OrdersController.ChangeStatus
      ▼
OrderService.ChangeStatusAsync
      ├─ [STATE] OrderState.For(status).CanMoveTo(target)
      ├─ StatusHistories.Add(...) → UpdateAsync
      └─ [OBSERVER] StatusChangedAsync
```

## 12. Uygulanmayan 4 desen (bilmen gereken)
Case 10 desenden 6'sını istiyor. Projede uygulanmayanlar ve bu projede nereye oturabilecekleri:

| Desen | Bu projede nereye oturur? |
|---|---|
| **Builder** | `OrderCreator` + `CalculateOrderAsync` yerine `new OrderBuilder().ForChannel(...).WithCustomer(...).AddItem(...).WithNote(...).Build()` |
| **Adapter** | `INotificationSender` arayüzü + `TwilioSmsAdapter`, `MockEmailAdapter`. Farklı dış servis API'lerini ortak bir arayüze uyarlamak. Observer'lar bunu kullanırdı. |
| **Specification** | `ActiveProducts()` ve admin sipariş filtreleri, `ISpecification<Order>` sınıflarına dönüşür: `StatusSpec`, `ChannelSpec`, `DateRangeSpec`; `And()`/`Or()` ile birleştirilir. |
| **Command** | Admin eylemleri sınıflara ayrılır: `ChangeStatusCommand`, `CancelOrderCommand` (`Execute()`), isteğe bağlı olarak `Undo()` ve komut geçmişi. |

---

## Kendini sına
1. Sepet sayfasında gösterilen tutar ile kaydedilen tutarın asla farklı olamayacağını hangi tasarım kararı garanti ediyor?
2. `detail.Product = product` satırı, ürün başka bir DbContext'ten gelseydi neden sorun çıkarırdı?
3. `SetNext` `this` döndürseydi zincir nasıl olurdu? Hangi halkalar çalışmazdı?
4. `ChannelInfoHandler`'daki `return;` zinciri durdurur mu?
5. Masa siparişinde neden `CustomerId` NULL? `Customer` nesnesi oluşturulduğu halde neden kaydedilmiyor?
6. Bir observer'ın `SaveChanges`'i patlarsa sıradaki observer neden de patlayabilir?
7. "Ready → Cancelled" geçişine izin vermek kaç dosyayı değiştirir?
8. Bu projedeki State uygulamasını klasik GoF State'e çevirmek için ne yapardın?
