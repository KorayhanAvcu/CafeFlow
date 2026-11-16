# CafeFlow: Projenin Tam Anlatımı

> Bu doküman, CafeFlow projesini **case'in öğretmek istediği şeylere göre** baştan sona anlatır: önce case'in ne istediği, sonra mimari ve Dependency Injection, sonra bir siparişin kodda izlediği yol, altı tasarım deseni (her biri gerçek kodla), iş kuralları, teknik beklentiler ve sunumda sorulabilecek sorular.
>
> Daha derin ayrıntı için: `docs/kod-rehberi/` (00–07), desenlerin özeti için: `docs/DESIGN_PATTERNS.md`.

## İçindekiler

1. Case ne istiyor?
2. Mimari: 5 proje, kim kimi tanıyor
3. Dependency Injection: projenin temeli
4. Case maddesi → kod haritası
5. Bir siparişin kodda yolculuğu
6. Altı tasarım deseni (kodla)
7. Transaction: Unit of Work
8. Diğer ekranlarda ne oluyor?
9. Case'in iş kuralları ve kodları
10. Teknik beklentiler: SOLID, validasyon, hata yönetimi, async, ilişki bütünlüğü, test
11. Hepsini bağlayan yer: DI kayıtları
12. Kodda kalan `new`'ler neden doğru?
13. Uygulanmayan 4 desen
14. Sunumda sorulabilecek sorular
15. Nasıl çalışmalısın?

---

## 1. Case ne istiyor?

Case'in odak cümlesi:

> *"Amaç daha fazla ekran eklemek değil; **değişebilir iş kurallarını** sürdürülebilir, **test edilebilir** ve **genişletilebilir** bir mimariyle çözmektir."*

Kabul koşulu:

> *"Yalnızca desen adı taşıyan bir interface veya sınıf yazmak yeterli değildir. Seçilen her desen uygulamanın **çalışan bir senaryosuna** entegre edilmelidir."*

Yani hocanın senden duymak istediği şey şu soruya cevap: **"Bu iş kuralı değişirse kodda neresi değişir, neresi değişmez?"** Bu dokümandaki her bölüm bu soruya göre yazıldı.

Case'in istedikleri kısaca:

| Konu | İstenen |
|---|---|
| Kanallar | Online teslimat (adres), Gel-Al (teslim alma saati), QR masa siparişi |
| Müşteri ekranları | Ana sayfa, ürünler (arama + kategori filtresi), ürün detay (boyut ve ek malzeme), sepet (miktar, sipariş türü, toplam) |
| Admin ekranları | Kategori yönetimi, sipariş yönetimi (liste, detay, durum güncelleme), ortak sidebar ve navbar |
| Desenler | 10 desenden en az 6'sı, gerçek iş akışında çalışarak |
| İş kuralları | Fiyat sunucuda hesaplanır, QR masa kaydıyla doğrulanır, farklı özelleştirmeler ayrı kalem, durum geçişlerinde kural |
| Teknik | Katmanlı yapı + DI, SOLID, validasyon, hata yönetimi, async, ilişki bütünlüğü, transaction |

Projede uygulanan altı desen (case'in önerdiği altılı): **Strategy, Factory Method, Observer, State, Decorator, Chain of Responsibility.**

---

## 2. Mimari: 5 proje, kim kimi tanıyor

```text
CafeOrder.WebUI            Controller, View, Session sepeti, Identity (admin girişi)
       ↓ tanır
CafeOrder.BusinessLayer    Servisler (iş kuralları) + 6 tasarım deseni + Mapster
       ↓ tanır
CafeOrder.DataAccessLayer  DbContext, repository'ler, Unit of Work, migration'lar
       ↓ tanır
CafeOrder.EntityLayer      Tablo sınıfları (Order, Product ...) ve enum'lar

CafeOrder.DtoLayer         Ekran ile servis arasında taşınan veri (ResultXDto, CreateXDto ...)
```

**Neden katmanlı?** Her katmanın tek bir işi var:

- **Controller** sadece "ne istendiğini" bilir ve servise iletir. İçinde iş kuralı (`if`) yoktur.
- **Servis** "hangi kurallarla" yapılacağını bilir.
- **Repository** "veritabanında nasıl" yapılacağını bilir. EF Core kodu (Include, Where, ToListAsync) sadece burada.

Veritabanını değiştirmek istesen sadece DataAccessLayer değişir; kurallar ve sayfalar etkilenmez.

**Neden DTO?** Entity'ler (veritabanı sınıfları) sayfaya hiç gitmez. Sayfa `ResultProductDto` görür, form `CreateOrderDto` gönderir. Böylece kullanıcı forma `Total=1` gibi bir alan ekleyerek fiyatı değiştiremez (buna *over-posting* denir). Entity ↔ DTO çevirisini Mapster yapar.

| DTO | Ne zaman |
|---|---|
| `ResultXDto` | Listeleme ve detay ekranları |
| `CreateXDto` | Yeni kayıt |
| `UpdateXDto` | Düzenleme formu |
| `CartItemDto` | Session'daki sepet satırı (sadece Id + seçenek Id'leri + adet, **fiyat yok**) |
| `CreateOrderDto` | Sipariş formu (ad, telefon, adres, not, saat) |

---

## 3. Dependency Injection: projenin temeli

### 3.1 Uygulama açılırken

`Program.cs` iki satırla bütün kayıtları yaptırır:

```csharp
builder.Services.AddDataAccessLayer(connectionString);    // DbContext, repository'ler, Unit of Work
builder.Services.AddBusinessLayer(builder.Configuration); // servisler, desenler, ayarlar, Mapster
```

Bu satırlar DI kabına **"biri X isterse ona Y'yi ver"** listesini yazar:

- `IOrderService` isteyen → `OrderService` alır.
- `IEnumerable<IDeliveryFeeStrategy>` isteyen → 3 stratejinin hepsini alır.

**Neden kayıtlar katmanlarda?** Her katman kendi sınıflarını kendisi kaydeder (`DataAccessServiceRegistration.cs`, `BusinessServiceRegistration.cs`). Yeni bir desen sınıfı eklediğinde BusinessLayer'daki kayıt dosyasına bir satır yazarsın; WebUI'ye dokunmazsın.

Açılışta ayrıca:

- Migration'lar uygulanır, veritabanı boşsa örnek menü eklenir, ilk admin hesabı oluşturulur.
- `PricingOptions` doğrulanır (Options pattern + `ValidateOnStart`). `appsettings.json`'a `"DeliveryFee": -5` yazarsan uygulama açılmaz ve hangi alanın yanlış olduğunu söyler.

### 3.2 Bir istek geldiğinde nesneler nasıl oluşur?

Hiçbir sınıf ihtiyacını `new` ile oluşturmaz; **constructor'da ister**:

```csharp
public CartController(ICartService cartService, IOrderQueryService orderQueryService, ITableService tableService)
```

`/cart/checkout` isteği gelince DI kabı şunu yapar:

```text
CartController lazım → ICartService istiyor
  → CartService lazım → IOrderService, IOrderPricingService, IProductService ... istiyor
     → OrderService lazım → IOrderRepository, IUnitOfWork, IEnumerable<OrderCreator> ... istiyor
        → repository'ler lazım → CafeOrderDbContext istiyorlar
           → bu istek için TEK bir DbContext oluşturulur ve hepsine aynısı verilir
```

İstek bitince scoped nesnelerin hepsi (DbContext, repository'ler, servisler) atılır.

**Neden `new` değil de DI?**

1. **Değiştirilebilirlik:** `OrderService` hangi strateji, hangi doğrulama kuralı olduğunu bilmiyor; hepsini liste olarak alıyor. Yeni kural = yeni sınıf + bir kayıt satırı.
2. **Test:** Testte gerçek veritabanı yerine sahte repository, gerçek saat yerine sabit saat verebilirsin.
3. **Aynı DbContext:** Bütün repository'ler aynı DbContext'i paylaştığı için tek `SaveChanges` hepsinin değişikliğini birlikte yazabiliyor (Unit of Work, Bölüm 7).

### 3.3 Ömürler

| Ömür | Ne demek | Projede kim |
|---|---|---|
| **Scoped** | Her HTTP isteğinde bir tane | DbContext, Unit of Work, repository'ler, servisler, observer'lar, `IMapper` |
| **Singleton** | Uygulama boyunca tek | Ücret stratejileri, creator'lar, `TimeProvider`, `IOptions<PricingOptions>` |
| **Transient** | Her istendiğinde yeni | Doğrulama halkaları |

- Stratejiler ve creator'lar **Singleton**: içlerinde değişen bir durum yok.
- Doğrulama halkaları **Transient**: `SetNext` halkanın içini değiştiriyor; paylaşılırlarsa iki istek birbirinin zincirini bozabilir.
- **Kural:** Bir sınıf kendinden kısa ömürlü bir şeye bağımlı olamaz (Singleton → Scoped yasak). Development ortamında ASP.NET Core bunu açılışta kontrol eder.

---

## 4. Case maddesi → kod haritası

| Case maddesi | Dosya |
|---|---|
| Strategy | `BusinessLayer/Pricing/Fees/` |
| Factory Method | `BusinessLayer/Creation/` |
| Observer | `BusinessLayer/Events/` |
| State | `BusinessLayer/States/OrderState.cs` |
| Decorator | `BusinessLayer/Pricing/` (`IPriceComponent`, `BaseProductPrice`, `OptionPriceDecorator`) |
| Chain of Responsibility | `BusinessLayer/Validation/` |
| Desenlerin sırayla çalıştığı yer | `BusinessLayer/Services/Orders/OrderService.cs` |
| Fiyat sunucuda hesaplanır | `BusinessLayer/Services/Orders/OrderPricingService.cs` |
| Farklı özelleştirme = ayrı kalem | `BusinessLayer/Services/Cart/CartService.cs` |
| QR doğrulama | `BusinessLayer/Services/Tables/TableService.cs` + `Validation/ChannelInfoHandler.cs` |
| Transaction | `DataAccessLayer/UnitOfWork/` |
| DI kayıtları | `BusinessLayer/BusinessServiceRegistration.cs`, `DataAccessLayer/DataAccessServiceRegistration.cs` |
| İlişki bütünlüğü | `DataAccessLayer/Context/Configurations/` |

Sipariş işleri dört servise bölünmüştür:

| Servis | Ne yapar |
|---|---|
| `OrderPricingService` | Sepeti fiyatlandırır: Decorator + Strategy. Kaydetmez. |
| `OrderQueryService` | Okuma: admin listesi, detay (+ State ile izinli durumlar), onay sayfası, sayılar |
| `OrderService` | Değiştirme: sipariş verme ve durum değiştirme. Desenleri sırayla çalıştırır. |
| `DashboardService` | Admin panel rakamları |

---

## 5. Bir siparişin kodda yolculuğu

Müşteri sepette **"Siparişi Ver"** düğmesine basınca:

```text
Views/Cart/Index.cshtml      form → POST /cart/checkout (CreateOrderDto)
  ↓
CartController.Checkout      sepeti, kanalı ve QR'ı Session'dan okur
  ↓
CartService.CheckoutAsync    kanalı formdan değil Session'dan alır
  ↓
OrderService.PlaceOrderAsync
  1. Mapster                 CreateOrderDto → Order
  2. Müşteri / masa          telefonla kayıtlı müşteri, QR ile aktif masa
  3. FACTORY METHOD          kanala uygun creator siparişi kurar
  4. DECORATOR + STRATEGY    OrderPricingService: birim fiyat, ara toplam, ücret
  5. CHAIN                   Ürün → Adet → Kanal bilgisi → Tutar
  6. OBSERVER                Mutfak, Kasa, Admin bildirimleri eklenir
  7. UNIT OF WORK            hepsi TEK transaction'da kaydedilir
  ↓
CartController               sepeti boşaltır → /cart/confirmation/{orderNo}
```

### 5.1 Kapı: `CartController.Checkout`

*Case: hata yönetimi, katmanlı yapı.*

```csharp
[HttpPost("checkout")]
[ValidateAntiForgeryToken]                                  // başka siteden sahte form gönderilemez
public async Task<IActionResult> Checkout(CreateOrderDto form)   // form → DTO (entity değil!)
{
    try
    {
        var order = await _cartService.CheckoutAsync(
            HttpContext.Session.GetCart(),        // sepet: sadece Id + adet, FİYAT YOK
            HttpContext.Session.GetChannel(),     // kanal formdan değil Session'dan
            HttpContext.Session.GetQrToken(),     // QR da Session'dan
            form);

        HttpContext.Session.ClearCart();
        return Redirect("/cart/confirmation/" + order.OrderNo);   // PRG: F5 ile tekrar sipariş olmaz
    }
    catch (BusinessException ex)                  // SADECE iş kuralı hatası yakalanır
    {
        return View("Index", await GetCartPageAsync(form, ex.Message));   // yazılanlar kaybolmaz
    }
}
```

- **Controller iş kuralı bilmiyor.** Hiçbir `if` yok; Session'ı okuyup servise veriyor.
- **Neden sadece `BusinessException`?** "Adres girin" gibi kullanıcının düzeltebileceği hatalar ekranda gösteriliyor. Veritabanı çöktü gibi sistem hataları yakalanmıyor ve `/error` sayfasına düşüyor; kullanıcıya SQL ayrıntısı sızmıyor.
- **Neden hata olunca Redirect yok?** Redirect yapılsaydı kullanıcının yazdığı ad, telefon ve adres kaybolurdu.
- **Neden kanal Session'dan?** Kullanıcı formu değiştirip "Teslimat" yerine "Masa" gönderemesin diye.

### 5.2 Orkestra şefi: `OrderService.PlaceOrderAsync`

*Case: "seçer → fiyat → kanal → doğrulama → kayıt → bilgilendirme".*

```csharp
public async Task<ResultOrderDto> PlaceOrderAsync(CreateOrderDto dto, List<CartItemDto> items, string? qrToken)
{
    Order form = _mapper.Map<Order>(dto);                                   // DTO → Entity (Mapster)
    form.Customer = await FindOrCreateCustomerAsync(dto.FullName, dto.Phone);

    Table? table = null;
    if (form.Channel == OrderChannel.Table && !string.IsNullOrWhiteSpace(qrToken))
    {
        string token = qrToken.ToUpper();
        table = await _tableRepository.GetFirstAsync(t => t.QrToken == token && t.IsActive);  // QR → AKTİF masa
    }

    Order order = GetCreator(form.Channel).Create(form, table);   // 1) FACTORY METHOD
    await _pricing.ApplyPricesAsync(order, items);                // 2) DECORATOR + STRATEGY
    _validation.Validate(order);                                  // 3) CHAIN (hata → exception)
    await _orderRepository.AddAsync(order);                       // 4) "eklenecek" diye işaretle
    await _events.OrderPlacedAsync(order);                        // 5) OBSERVER
    await _unitOfWork.SaveChangesAsync();                         // 6) TEK TRANSACTION
    return _mapper.Map<ResultOrderDto>(order);
}
```

Case'in örnek süreci bu metotta satır satır var. Sunumda bu metodu gösterip her satırın hangi desen olduğunu anlat.

Constructor'ı: bu sınıf hiçbir şeyi `new`'lemiyor, her şeyi DI'dan alıyor.

```csharp
public OrderService(
    IOrderRepository orderRepository,
    ITableRepository tableRepository,
    ICustomerRepository customerRepository,
    IEnumerable<OrderCreator> creators,        // bütün Factory'ler
    IOrderPricingService pricing,              // Decorator + Strategy
    IOrderValidationChain validation,          // Chain
    IOrderEventPublisher events,               // Observer
    IUnitOfWork unitOfWork,                    // Transaction
    TimeProvider time,
    IMapper mapper)
```

`OrderService` hangi kanalların, hangi kuralların, hangi bildirimlerin olduğunu **bilmiyor**. Bu yüzden onlar değiştiğinde bu dosya değişmiyor. Case'in *"değişebilir iş kuralları"* cümlesinin koddaki karşılığı bu.

Müşteri bulma yardımcısı:

```csharp
private async Task<Customer> FindOrCreateCustomerAsync(string? fullName, string? phone)
{
    Customer customer = new Customer();
    customer.FullName = (fullName ?? "").Trim();
    customer.Phone = (phone ?? "").Trim();
    if (customer.Phone != "")
    {
        string p = customer.Phone;
        var existing = await _customerRepository.GetFirstAsync(c => c.Phone == p);
        if (existing != null) return existing;     // aynı telefon → aynı müşteri
    }
    return customer;                               // yeni müşteri: siparişle birlikte INSERT edilecek
}
```

---

## 6. Altı tasarım deseni (kodla)

Her desen aynı sırayla anlatılıyor: case'teki iş problemi → desen olmasaydı ne olurdu → kod → hangi ekranda çalışıyor → kural değişirse ne değişir.

### 6.1 Decorator: seçenekler fiyatı sarıyor

**Case:** *"Kahveye ekstra shot, farklı süt, şurup veya krema eklenmesi ve toplam fiyatın hesaplanması."*

**Desen olmasaydı:** Fiyat hesabı tek bir metotta `if (büyük) fiyat += 30; if (yulaf) fiyat += 15; ...` diye büyürdü. Yüzdelik bir seçenek gelince bu metot karmaşıklaşırdı.

**Kod** (`BusinessLayer/Pricing/`):

```csharp
public interface IPriceComponent { decimal GetPrice(); }          // ortak arayüz

public class BaseProductPrice : IPriceComponent                  // EN İÇTEKİ kutu
{
    private readonly decimal _basePrice;
    public BaseProductPrice(decimal basePrice) { _basePrice = basePrice; }
    public decimal GetPrice() { return _basePrice; }
}

public class OptionPriceDecorator : IPriceComponent              // SARAN kutu
{
    private readonly IPriceComponent _inner;                      // kendisi gibi bir şeyi tutuyor!
    private readonly decimal _extraPrice;
    public OptionPriceDecorator(IPriceComponent inner, decimal extraPrice) { _inner = inner; _extraPrice = extraPrice; }
    public decimal GetPrice() { return _inner.GetPrice() + _extraPrice; }   // içtekine sor + ekle
}
```

Kullanıldığı yer (`OrderPricingService`):

```csharp
private static decimal CalculateUnitPrice(Product product, List<ProductOption> options)
{
    IPriceComponent price = new BaseProductPrice(product.BasePrice);   // 160
    foreach (ProductOption option in options)
        price = new OptionPriceDecorator(price, option.PriceDelta);    // sar, sar, sar
    return price.GetPrice();
}
```

Bellekte oluşan yapı:

```text
Shot(+20) ( Yulaf(+15) ( Büyük(+30) ( Taban(160) ) ) )
GetPrice() → 160 → 190 → 205 → 225 ₺
```

- **Kilit nokta:** `OptionPriceDecorator` hem `IPriceComponent` **oluyor** hem bir `IPriceComponent` **tutuyor**. Bu yüzden kutular istediğin kadar iç içe girebiliyor.
- **Ekranda:** Ürün detayında seçenek seçip sepete ekleyince; sepet sayfasında ve sipariş verirken.
- **Kural değişirse:** "Büyük boy %20 artırsın" → yeni bir `PercentPriceDecorator` sınıfı. Hesap kodu değişmez.

### 6.2 Strategy: kanala göre ücret

**Case:** *"Teslimat, gel-al ve masadan sipariş kanalları için farklı ücret hesaplama kuralları."*

**Desen olmasaydı:** `if (kanal == Teslimat) ... else if (GelAl) ...` zinciri olurdu; yeni bir kanal için bu metodu açıp değiştirmek gerekirdi.

**Kod** (`BusinessLayer/Pricing/Fees/`):

```csharp
public interface IDeliveryFeeStrategy
{
    OrderChannel Channel { get; }              // "ben hangi kanalın kuralıyım"
    decimal Calculate(decimal orderAmount);
}

public class DeliveryFeeStrategy : IDeliveryFeeStrategy
{
    private readonly PricingOptions _options;
    public DeliveryFeeStrategy(IOptions<PricingOptions> options) { _options = options.Value; }   // rakamlar appsettings'ten
    public OrderChannel Channel => OrderChannel.Delivery;
    public decimal Calculate(decimal orderAmount)
    {
        if (orderAmount >= _options.FreeDeliveryFrom) return 0;   // 600 ₺ ve üzeri ücretsiz
        return _options.DeliveryFee;                              // 30 ₺
    }
}

public class PickupFeeStrategy : IDeliveryFeeStrategy
{
    public OrderChannel Channel => OrderChannel.Pickup;
    public decimal Calculate(decimal orderAmount) { return 0; }
}

public class TableFeeStrategy : IDeliveryFeeStrategy
{
    public OrderChannel Channel => OrderChannel.Table;
    public decimal Calculate(decimal orderAmount) { return 0; }
}
```

Seçim (`OrderPricingService`):

```csharp
// constructor: DI bütün stratejileri verdi, kanala göre sözlüğe konuyor
_feeStrategies = feeStrategies.ToDictionary(s => s.Channel);

// hesapta
order.DeliveryFee = order.Details.Count == 0 ? 0 : GetFeeStrategy(order.Channel).Calculate(order.Subtotal);
```

- **Ekranda:** Sepette sipariş türü seçilince. Örnek: 1 × Cold Brew ₺155, Teslimat → ücret ₺30, toplam ₺185.
- **Kural değişirse:**
  - Rakam değişirse (ücret 40 ₺) → sadece `appsettings.json`. Kod değişmez.
  - Yeni kanal → yeni bir strateji sınıfı + kayıt dosyasına bir satır. Servis değişmez.
- **Sunum cümlesi:** *"Servis hangi stratejinin çalıştığını bilmiyor; sadece `Calculate` diyor. Yeni kanal = yeni sınıf + bir kayıt satırı. Bu Open/Closed."*

### 6.3 Factory Method: kanala göre sipariş nesnesi

**Case:** *"Online, gel-al ve masa siparişleri için türe özgü nesne oluşturma akışları."*

**Desen olmasaydı:** Tek bir metotta "teslimatsa adresi, gel-al'sa saati, masaysa masayı kopyala" karışık dururdu; yanlış kanala yanlış alan girebilirdi.

**Kod** (`BusinessLayer/Creation/`):

```csharp
public abstract class OrderCreator
{
    private readonly TimeProvider _time;
    protected OrderCreator(TimeProvider time) { _time = time; }

    public abstract OrderChannel Channel { get; }          // "ben hangi kanalın creator'ıyım"

    public Order Create(Order form, Table? table)          // ORTAK İSKELET (değiştirilemez)
    {
        Order order = CreateForChannel(form, table);       // ← türe özgü kısım ALT SINIFTA
        order.OrderNo = CreateOrderNo();                   // ortak: CF-1A2B3C4D
        order.Status = OrderStatus.Received;               // ortak: "Alındı"
        order.Note = form.Note;
        order.CreatedAt = _time.GetLocalNow().DateTime;    // ortak: tarih

        OrderStatusHistory history = new OrderStatusHistory();   // ortak: ilk durum kaydı
        history.ToStatus = OrderStatus.Received;
        history.ChangedAt = order.CreatedAt;
        order.StatusHistories.Add(history);
        return order;
    }

    protected abstract Order CreateForChannel(Order form, Table? table);   // ← FACTORY METHOD
}

public class DeliveryOrderCreator : OrderCreator
{
    public DeliveryOrderCreator(TimeProvider time) : base(time) { }
    public override OrderChannel Channel => OrderChannel.Delivery;

    protected override Order CreateForChannel(Order form, Table? table)
    {
        Order order = new Order();
        order.Channel = OrderChannel.Delivery;
        order.Customer = form.Customer;      // teslimat: müşteri
        order.Address = form.Address;        // teslimat: adres
        order.AddressNotes = form.AddressNotes;
        return order;                        // alma saati, masa → BİLEREK kopyalanmıyor
    }
}
// PickupOrderCreator → müşteri + alma saati, TableOrderCreator → masa (TableId)
```

Seçim (`OrderService`):

```csharp
_creators = creators.ToDictionary(c => c.Channel);           // constructor'da
Order order = GetCreator(form.Channel).Create(form, table);  // sipariş verirken
```

- **`CreateForChannel`** asıl Factory Method: üst sınıf "bir sipariş lazım" diyor, alt sınıf "nasıl kurulacağını" biliyor.
- **`Create`** bir Template Method: adımların sırası sabit, sadece değişen adım alt sınıfa bırakılmış.
- **Beyaz liste:** Her alt sınıf sadece kendi alanlarını kopyalıyor; teslimat siparişine alma saati giremez.
- **Sipariş numarası rastgele** (GUID'in ilk 8 karakteri): sıralı olsaydı biri `/cart/confirmation/CF-0002` yazıp başkasının siparişini görebilirdi.
- **Kural değişirse:** Yeni kanal = yeni bir creator sınıfı + bir kayıt satırı.

### 6.4 Chain of Responsibility: doğrulama halkaları

**Case:** *"Ürün uygunluğu, miktar, masa/adres ve sipariş tutarı doğrulamalarının zincir halinde çalışması."*

Case'teki dört madde, projedeki dört halkayla birebir aynı:

| Case maddesi | Halka | Örnek hata mesajı |
|---|---|---|
| Ürün uygunluğu | `ProductHandler` | Sepetiniz boş. / ... artık satışta değil. |
| Miktar | `QuantityHandler` | Adet 1 ile 20 arasında olmalı. |
| Masa/adres | `ChannelInfoHandler` | Lütfen teslimat adresinizi girin. / Lütfen masanızdaki QR kodu okutun. |
| Sipariş tutarı | `OrderAmountHandler` | Teslimat için en az sipariş tutarı 100 ₺. |

**Halka iskeleti** (`OrderValidationHandler.cs`):

```csharp
public abstract class OrderValidationHandler
{
    private OrderValidationHandler? _next;                   // sıradaki halka

    public OrderValidationHandler SetNext(OrderValidationHandler next) { _next = next; return next; }

    public void Handle(Order order)
    {
        Check(order);                                        // 1) kendi kuralım (hata → exception, zincir durur)
        if (_next != null) _next.Handle(order);              // 2) sorun yoksa sıradakine ver
    }

    protected abstract void Check(Order order);              // her halka sadece bunu yazar
}
```

**Bir halka** (`ChannelInfoHandler.cs`, case'teki "masa/adres" maddesi):

```csharp
protected override void Check(Order order)
{
    if (order.Channel == OrderChannel.Table)
    {
        if (order.TableId == null) throw new BusinessException("Lütfen masanızdaki QR kodu okutun.");
        return;                                    // masada ad/telefon sorulmaz
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
}
```

**Zinciri kuran sınıf** (`OrderValidationChain.cs`):

```csharp
public OrderValidationChain(IEnumerable<OrderValidationHandler> handlers)   // DI kayıt sırasıyla verir
{
    OrderValidationHandler? previous = null;
    foreach (OrderValidationHandler handler in handlers)
    {
        if (previous == null) _first = handler;      // Ürün
        else previous.SetNext(handler);              // Ürün → Adet → Kanal → Tutar
        previous = handler;
    }
}

public void Validate(Order order) => _first?.Handle(order);
```

- Her halka **tek** bir kural; halkalar birbirini tanımıyor, sadece "sıradaki" var.
- Bir halka hata bulursa exception fırlıyor; sonraki halkalar ve kayıt hiç çalışmıyor. Veritabanına sıfır satır gidiyor.
- **Kayıt sırası = zincir sırası.** Tutar en sonda, çünkü ara toplamın hesaplanmış olması lazım.
- **Kural değişirse:** "Gece 23:00–07:00 teslimat yok" → yeni bir halka sınıfı + kayıt listesine bir satır. Diğer halkalar ve `OrderService` değişmez.

### 6.5 State: izinli durum geçişleri

**Case:** *"Alındı → Hazırlanıyor → Hazır → Teslim Edildi / İptal durumları arasında izinli geçişler."*

**Desen olmasaydı:** Servis ve sayfalarda `if (durum == Alındı && hedef == Hazırlanıyor || ...)` gibi dağınık kontroller olurdu.

```text
Alındı ──► Hazırlanıyor ──► Hazır ──► Teslim Edildi (son)
   │             │
   └─────┬───────┘
         ▼
       İptal (son)
```

**Kod** (`BusinessLayer/States/OrderState.cs`):

```csharp
public abstract class OrderState
{
    public abstract List<OrderStatus> GetAllowedNext();                       // "nereye gidebilirim?"
    public bool CanMoveTo(OrderStatus target) { return GetAllowedNext().Contains(target); }

    public static OrderState For(OrderStatus status)                          // enum → durum nesnesi
    {
        if (status == OrderStatus.Received)  return new ReceivedState();
        if (status == OrderStatus.Preparing) return new PreparingState();
        if (status == OrderStatus.Ready)     return new ReadyState();
        if (status == OrderStatus.Delivered) return new DeliveredState();
        return new CancelledState();
    }
}

// ReceivedState  → Hazırlanıyor, İptal
// PreparingState → Hazır, İptal
// ReadyState     → Teslim Edildi
// DeliveredState, CancelledState → (yok, son durum)
```

Kullanıldığı yer (`OrderService.ChangeStatusAsync`):

```csharp
var order = await _orderRepository.GetWithDetailsAsync(id);

OrderState state = OrderState.For(order.Status);
if (!state.CanMoveTo(target))
    throw new BusinessException("Sipariş \"" + order.Status.ToText() + "\" durumundan \"" + target.ToText() + "\" durumuna geçemez.");

OrderStatus previous = order.Status;
order.Status = target;
order.StatusHistories.Add(new OrderStatusHistory { FromStatus = previous, ToStatus = target, ... });   // geçmiş
await _events.StatusChangedAsync(order, previous);    // OBSERVER
await _unitOfWork.SaveChangesAsync();                 // tek kayıt
```

Aynı kural admin sayfasında da kullanılıyor (`OrderQueryService.GetByIdAsync`):

```csharp
dto.AllowedNextStatuses = OrderState.For(order.Status).GetAllowedNext();   // sadece bunların düğmesi çizilir
```

- **İki tüketici:** Servis izinsiz geçişi reddediyor (gerçek güvenlik; form elle gönderilse bile engellenir). Sayfa sadece izinli düğmeleri çiziyor (kullanıcı deneyimi).
- **Kural değişirse:** "Hazır sipariş de iptal edilebilsin" → `ReadyState`'e tek bir değer eklenir; iptal düğmesi sayfada kendiliğinden çıkar.
- **Dürüst not:** Bu, State'in hafif "geçiş tablosu" uygulaması. Klasik GoF State'te davranış da durum sınıfına taşınır (ör. `ReadyState` müşteriye "siparişiniz hazır" der). Case "izinli geçişler" istediği için yeterli.

### 6.6 Observer: mutfak, kasa ve admin bildirimleri

**Case:** *"Sipariş veya durum değişikliğinde mutfak, kasa ve bildirim bileşenlerinin haberdar edilmesi."*

**Desen olmasaydı:** `OrderService` içinde "mutfağa yaz, kasaya yaz, admine yaz" diye üç ayrı kod olurdu; SMS eklemek için servisi değiştirmek gerekirdi.

**Yayıncı** (`OrderEventPublisher.cs`):

```csharp
public class OrderEventPublisher : IOrderEventPublisher
{
    private readonly IEnumerable<IOrderObserver> _observers;   // abone listesi = DI kayıtları

    public async Task OrderPlacedAsync(Order order)
    {
        foreach (IOrderObserver observer in _observers)
        {
            try { await observer.OrderPlacedAsync(order); }
            catch (Exception ex) { _logger.LogError(ex, "{OrderNo} siparişi için bildirim yazılamadı", order.OrderNo); }
        }
    }
}
```

**Bir abone** (`KitchenObserver.cs`):

```csharp
public async Task OrderPlacedAsync(Order order)
{
    await SendAsync(order, $"Yeni sipariş {order.OrderNo}: hazırlanacak {order.Details.Sum(d => d.Quantity)} ürün.");
}

public async Task StatusChangedAsync(Order order, OrderStatus previous)
{
    if (order.Status == OrderStatus.Cancelled)                 // mutfak SADECE iptalle ilgileniyor
        await SendAsync(order, $"{order.OrderNo} siparişi iptal edildi. Hazırlamayı durdurun.");
}

private async Task SendAsync(Order order, string message)
{
    Notification notification = new Notification();
    notification.Order = order;                                // Id henüz yok; EF kayıtta bağlar
    notification.Target = NotificationTarget.Kitchen;
    notification.Message = message;
    notification.CreatedAt = _time.GetLocalNow().DateTime;
    await _notificationRepository.AddAsync(notification);      // sadece işaretle; servis kaydedecek
}
```

| Gözlemci | Yeni sipariş | Durum değişikliği |
|---|---|---|
| `KitchenObserver` | hazırlanacak ürün sayısı | sadece iptal |
| `CashierObserver` | toplam tutar | sadece teslim |
| `AdminObserver` | her yeni sipariş | her değişiklik |

- **Kilit nokta:** `OrderService` mutfağı tanımıyor, sadece `_events.OrderPlacedAsync(order)` diyor. Hangi olayla ilgileneceğine abone kendisi karar veriyor.
- Bildirimler `Notifications` tablosuna yazılıyor; admin panelindeki zilde görünüyor. Case buna izin veriyor: *"Gerçek harici servis yerine açıkça belirtilmiş simülasyonlar."*
- **Kural değişirse:** SMS bildirimi = yeni bir `SmsObserver` + bir kayıt satırı. `OrderService` haberi bile olmaz.

---

## 7. Transaction: Unit of Work

*Case: "sipariş kayıtlarında transaction yaklaşımı".*

```csharp
// Repository: KAYDETMİYOR, sadece işaretliyor (GenericRepository)
public async Task AddAsync(TEntity entity) { await Set.AddAsync(entity); }
public void Remove(TEntity entity) { Set.Remove(entity); }

// Unit of Work: hepsini TEK seferde yazıyor (EfUnitOfWork)
public Task<int> SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);
```

**Neden çalışıyor?** Bir istekteki bütün repository'ler **aynı DbContext'i** paylaşıyor (Scoped). Sipariş, kalemler, seçenekler, ilk durum kaydı, yeni müşteri ve 3 bildirim aynı DbContext'e ekleniyor; tek `SaveChangesAsync` hepsini **tek transaction'da** yazıyor. Ya hepsi kaydedilir ya hiçbiri.

**Kaydetme kararı serviste, repository'de değil.** Repository "veriye nasıl erişilir" bilir; "bu iş nerede bitiyor" iş kuralıdır.

**İzlenen kayıt için `Update` gerekmez.** Veritabanından okunan bir kayıt değiştirilince EF Core bunu kendisi fark eder (change tracker) ve sadece değişen kolonu UPDATE eder. Bu yüzden `ChangeStatusAsync` `Update(order)` çağırmıyor.

Örnek: Teslimat siparişi CF-3324BDC7 verildiğinde tek `SaveChanges` ile yazılanlar: 1 müşteri, 1 sipariş, 1 kalem, 1 durum kaydı ve 3 bildirim (Kitchen, Cashier, Admin).

---

## 8. Diğer ekranlarda ne oluyor?

### 8.1 Menü sayfası (`GET /products`)

```text
ProductsController.Index(category, q)
  → ProductService.GetMenuAsync(category, q)
     → ProductRepository.GetMenuAsync(...)     ← SQL burada kurulur
        Active(): ürün aktif VE kategorisi aktif
        + kategori filtresi + arama (LIKE)
     ← List<Product>
  ← Mapster → List<ResultProductDto>
→ View(products) → HTML
```

Sorgu repository'de: servis sadece `GetMenuAsync` gibi isimli bir metot çağırıyor. BusinessLayer'da tek satır EF Core kodu yok.

### 8.2 Sepete ekle (`POST /cart/add`)

```text
CartController.Add(AddToCartDto dto)
  1. Session'dan sepeti oku
  2. CartService.AddItemAsync(cart, dto)
       - ürün aktif mi? (değilse BusinessException)
       - özelleştirilemeyen üründe seçenekleri at
       - aynı ürün + aynı seçenekler varsa adedi artır, yoksa yeni satır
       - adet 1–20 arasına sıkıştır
  3. Sepeti Session'a geri yaz
  4. TempData'ya "sepete eklendi" mesajı
  5. Redirect (geldiği sayfaya)   ← PRG: F5 ile ürün iki kez eklenmez
```

### 8.3 Sepet sayfası ve sipariş türü seçimi

```text
CartController.Channel(Delivery)  → Session'a "Delivery" yaz → Redirect /cart
CartController.Index
  → CartService.GetCartAsync(sepet, kanal, qr, ...)
     → OrderPricingService.CalculateAsync(Delivery, sepet)
        → ApplyPricesAsync: ürünler DB'den → DECORATOR → ara toplam → STRATEGY
        ← ResultOrderDto  (veritabanına HİÇ yazılmaz)
```

**En önemli tasarım fikri:** Sepet sayfası ve gerçek sipariş **aynı** `ApplyPricesAsync` metodunu kullanıyor. Müşterinin sepette gördüğü tutar ile kaydedilen tutar asla farklı olamaz.

### 8.4 Admin sipariş durumunu değiştiriyor

```text
OrdersController.ChangeStatus(id, Ready, note)
  try {
    OrderService.ChangeStatusAsync
      1. Siparişi ayrıntılarıyla oku
      2. STATE: CanMoveTo(Ready)?   (izinsizse BusinessException)
      3. Status = Ready, geçmişe "Hazırlanıyor → Hazır" satırı
      4. OBSERVER: Admin bildirimi (iptalde Mutfak, teslimde Kasa da)
      5. UNIT OF WORK: tek SaveChanges
    Redirect /admin/orders/{id}
  }
  catch (BusinessException) { detay sayfası + uyarı }
```

`OrdersController` iki servis istiyor: okuma için `IOrderQueryService`, durum değiştirmek için `IOrderService`.

### 8.5 Admin sipariş listesi ve panel

```text
OrderQueryService.GetAdminListAsync(filter)
  durum sayıları          → SQL GROUP BY
  filtreye uyan toplam    → SQL COUNT
  bu sayfanın 10 siparişi → SQL OFFSET/FETCH

DashboardService.GetDashboardAsync()
  bugünkü sipariş sayısı  → SQL COUNT
  bugünkü/haftalık ciro   → SQL SUM (iptaller hariç)
  en çok satan 5 ürün     → SQL GROUP BY + TOP 5
  son 6 sipariş           → sadece 6 kayıt
```

Bütün siparişler belleğe çekilmiyor; veritabanından sadece rakamlar ve gösterilecek kayıtlar geliyor. Sipariş sayısı arttıkça sayfalar yavaşlamıyor.

---

## 9. Case'in iş kuralları ve kodları

### 9.1 "Fiyatlar sunucuda yeniden hesaplanmalı"

Session'daki sepette fiyat **hiç yok**; sadece ürün Id'si, seçenek Id'leri ve adet var. Her hesapta fiyat veritabanından okunuyor (`OrderPricingService.ApplyPricesAsync`):

```csharp
var products = await _productRepository.GetByIdsWithCategoryAsync(items.Select(i => i.ProductId));  // fiyat DB'den
var allOptions = await _productOptionRepository.GetAllAsync();
...
detail.ProductName = product.Name;       // o anki adın kopyası
detail.BasePrice = product.BasePrice;    // o anki fiyatın kopyası
detail.UnitPrice = unitPrice;            // Decorator'ın hesapladığı
detail.LineTotal = unitPrice * item.Quantity;
```

Sahte bir seçenek Id'si gönderilirse veritabanındaki listede bulunamaz ve yok sayılır. Aynı seçenek iki kez gönderilse bile bir kez sayılır.

### 9.2 "QR bağlantıları masa kaydıyla doğrulanmalı"

```csharp
// QR okutulunca (TableService.GetByQrAsync)
string token = qrToken.Trim().ToUpper();
var table = await _tableRepository.GetFirstAsync(t => t.QrToken == token && t.IsActive);   // sadece AKTİF masa
```

- Sipariş anında QR **bir kez daha** kontrol ediliyor (`OrderService.PlaceOrderAsync`): masa arada pasif yapılmış olabilir.
- Masa bulunamazsa Chain'deki `ChannelInfoHandler` siparişi durduruyor.
- QR kodları rastgele (`CF-T07-K4P9XZ`); masa numarasından tahmin edilemiyor.

### 9.3 "Aynı ürünün farklı özelleştirmeleri ayrı sipariş kalemi"

`CartService.AddItemAsync`:

```csharp
options.Sort();                                       // [7,5] ile [5,7] aynı sayılsın
CartItemDto? sameLine = cart.FirstOrDefault(item =>
    item.ProductId == dto.ProductId && item.OptionIds.SequenceEqual(options));
if (sameLine != null)
{
    sameLine.Quantity = Math.Min(MaxQuantity, sameLine.Quantity + quantity);   // AYNI seçenekler → adet artar
}
else
{
    CartItemDto newLine = new CartItemDto();                                    // FARKLI seçenekler → yeni satır
    newLine.ProductId = dto.ProductId;
    newLine.OptionIds = options;
    newLine.Quantity = quantity;
    cart.Add(newLine);
}
```

Her sepet satırı siparişte ayrı bir `OrderDetail` oluyor.

### 9.4 "Durum geçişlerinde iş kuralları uygulanmalı"

State deseni (Bölüm 6.5). Her geçiş `OrderStatusHistories` tablosuna "önceki → yeni" ve isteğe bağlı notla yazılıyor.

---

## 10. Teknik beklentiler

### 10.1 SOLID: her harf için projeden bir örnek

| Harf | İlke | Projede |
|---|---|---|
| **S** | Single Responsibility: her sınıfın tek işi | Sipariş işleri 4 servise bölündü (fiyat, okuma, panel, değiştirme). Her doğrulama halkası tek bir kural. |
| **O** | Open/Closed: genişlemeye açık, değişime kapalı | Yeni kanal, kural veya bildirim = yeni sınıf + bir kayıt satırı. Servis kodu değişmiyor. |
| **L** | Liskov: alt sınıf üst sınıfın yerine geçebilir | Her `IDeliveryFeeStrategy` ve her `OrderCreator` birbirinin yerine kullanılabiliyor. |
| **I** | Interface Segregation: kullanmadığın metoda bağımlı olma | Sepet paneli sadece `IOrderPricingService`'i, sol menü sadece `IOrderQueryService`'i istiyor. |
| **D** | Dependency Inversion: arayüze bağımlı ol | Servisler `IOrderRepository`, `IUnitOfWork`, `IOrderEventPublisher` istiyor; EF Core'u ve somut sınıfları tanımıyor. |

### 10.2 Validasyon (iki katmanlı savunma)

1. **Ön katman:** `CartService` adedi 1–20 arasına sıkıştırıyor, özelleştirilemeyen üründe seçenekleri atıyor.
2. **Asıl katman:** Sunucudaki Chain. Biri formu elle değiştirse bile buraya takılır.

Admin tarafında her servisin bir `CheckAsync`'i var (ad uzunluğu, fiyat aralığı, aynı isim var mı...).

### 10.3 Hata yönetimi

- `BusinessException` = kullanıcının düzeltebileceği hata. Controller yakalıyor, mesaj ekranda gösteriliyor, form kaybolmuyor.
- Diğer bütün hatalar yakalanmıyor ve `/error` sayfasına düşüyor; kullanıcıya SQL veya stack trace gösterilmiyor.
- Observer'da bir dinleyici hata verirse loglanıyor, diğerleri yine çalışıyor.

### 10.4 Async veri erişimi

Bütün veritabanı işlemleri `async`/`await` (`ToListAsync`, `SaveChangesAsync`). Sorgular **sırayla** await ediliyor, paralel değil: hepsi aynı DbContext'i kullanıyor ve DbContext aynı anda iki işi kaldırmaz (thread-safe değil).

### 10.5 İlişki bütünlüğü

```csharp
// OrderDetailConfiguration
.OnDelete(DeleteBehavior.Restrict);            // siparişte geçen ürün silinemez

// OrderConfiguration / TableConfiguration / CategoryConfiguration
builder.HasIndex(x => x.OrderNo).IsUnique();   // aynı sipariş numarası iki kez olamaz
builder.HasIndex(x => x.QrToken).IsUnique();
builder.HasIndex(x => x.Slug).IsUnique();
```

- Ürünü olan kategori, siparişi olan ürün ve masa silinemez. Servis önce kibar bir mesajla ("geçmiş siparişlerde var, pasif yapın") yakalıyor; veritabanı kuralı ikinci savunma hattı.
- Sipariş kalemi ürün adının ve fiyatının **o anki kopyasını** tutuyor (snapshot); ürün sonradan değişse de eski sipariş bozulmuyor.

### 10.6 Test edilebilirlik

Desenler veritabanına bağımlı olmadığı için veritabanı olmadan test edilebiliyor:

- `OrderState`: "Hazırlanıyor → Teslim Edildi reddedilir mi?"
- `DeliveryFeeStrategy`: 599 ₺ → 30, 600 ₺ → 0. (`Options.Create(new PricingOptions { ... })` ile ayar verilir.)
- `ChannelInfoHandler`: saat `TimeProvider` ile sabitlenip "geçmiş saat reddedilir" test edilebilir. `DateTime.Now` ile bu mümkün değildi; bu yüzden değiştirildi.

**Dürüst not:** Projede henüz bir birim test projesi yok; ama mimari yazmaya hazır.

---

## 11. Hepsini bağlayan yer: DI kayıtları

`BusinessLayer/BusinessServiceRegistration.cs`:

```csharp
// Ayarlar ve zaman
services.AddOptions<PricingOptions>().Bind(configuration.GetSection("Pricing"))
        .ValidateDataAnnotations().ValidateOnStart();
services.TryAddSingleton(TimeProvider.System);

// STRATEGY
services.AddSingleton<IDeliveryFeeStrategy, DeliveryFeeStrategy>();
services.AddSingleton<IDeliveryFeeStrategy, PickupFeeStrategy>();
services.AddSingleton<IDeliveryFeeStrategy, TableFeeStrategy>();

// FACTORY METHOD
services.AddSingleton<OrderCreator, DeliveryOrderCreator>();
services.AddSingleton<OrderCreator, PickupOrderCreator>();
services.AddSingleton<OrderCreator, TableOrderCreator>();

// CHAIN: KAYIT SIRASI = ZİNCİR SIRASI
services.AddTransient<OrderValidationHandler, ProductHandler>();
services.AddTransient<OrderValidationHandler, QuantityHandler>();
services.AddTransient<OrderValidationHandler, ChannelInfoHandler>();
services.AddTransient<OrderValidationHandler, OrderAmountHandler>();
services.AddScoped<IOrderValidationChain, OrderValidationChain>();

// OBSERVER
services.AddScoped<IOrderObserver, KitchenObserver>();
services.AddScoped<IOrderObserver, CashierObserver>();
services.AddScoped<IOrderObserver, AdminObserver>();
services.AddScoped<IOrderEventPublisher, OrderEventPublisher>();

// Servisler
services.AddScoped<IOrderPricingService, OrderPricingService>();
services.AddScoped<IOrderQueryService, OrderQueryService>();
services.AddScoped<IOrderService, OrderService>();
services.AddScoped<IDashboardService, DashboardService>();
...
```

**Aynı arayüze birden fazla kayıt** bu projenin en önemli DI tekniği:

- `IOrderObserver` istersen **son kayıt** gelir.
- `IEnumerable<IOrderObserver>` istersen **hepsi**, kayıt sırasıyla gelir.

Dört desen de bunu kullanıyor. Case'in *"genişletilebilir"* kelimesinin kanıtı bu tablo:

| Yeni kural | Yeni dosya | Kayıt dosyasına eklenen | Değişen servis |
|---|---|---|---|
| Yeni kanal ücreti | 1 strateji | 1 satır | yok |
| Gece teslimat yasağı | 1 halka | 1 satır | yok |
| SMS bildirimi | 1 observer | 1 satır | yok |
| Yeni sipariş kanalı | 1 creator + 1 strateji | 2 satır | yok |

---

## 12. Kodda kalan `new`'ler neden doğru?

| `new` | Neden DI değil |
|---|---|
| `new Order()`, `new Notification()`, `new OrderDetail()` | Veri nesnesi, servis değil |
| `new OptionPriceDecorator(price, 15)` | Çalışma anındaki değeri sarıyor; her kalemde kaç kat olacağı ve değerleri farklı |
| `OrderState.For(status)` | Durum sınıfları bağımlılıksız ve içlerinde durum tutmuyor |

**Kural:** Bağımlılığı olan ve iş yapan sınıf DI'dan gelir. Veri taşıyan veya çalışma anındaki değerlerden kurulan nesne `new` ile oluşturulur.

---

## 13. Uygulanmayan 4 desen

Case 6 desen istiyor ve önerilen altılı uygulandı. Diğerlerinin projede nereye oturacağını bilmek yeterli:

| Desen | Bu projede nereye oturur |
|---|---|
| **Builder** | Sipariş adım adım kurulabilirdi: `new OrderBuilder().ForChannel(...).WithCustomer(...).AddItem(...).Build()`. Şu an bu işi Factory Method + `ApplyPricesAsync` yapıyor. |
| **Adapter** | `INotificationSender` arayüzü + `SmsAdapter`, `MockEmailAdapter`. Farklı dış servisleri ortak bir arayüze uyarlar; observer'lar bunu kullanırdı. |
| **Specification** | Admin sipariş filtreleri (durum, kanal, tarih, tutar) birleştirilebilir sınıflara dönüşürdü: `StatusSpec.And(ChannelSpec)`. Şu an `OrderRepository.Filter(...)` bu işi yapıyor ve Specification'a en yakın yer orası. |
| **Command** | Admin eylemleri (`CancelOrderCommand`, `ChangeStatusCommand`) ayrı sınıflar olurdu; geri alma (undo) ve eylem geçmişi eklenebilirdi. |

---

## 14. Sunumda sorulabilecek sorular

**"Desenin sadece adı mı var, gerçekten çalışıyor mu?"**
> "Hepsi gerçek akışta çalışıyor. Sepette Teslimat seçince Strategy 30 ₺ ekliyor. Adressiz sipariş verince Chain durduruyor. Sipariş verince Observer mutfağa, kasaya ve admine bildirim yazıyor; admin panelindeki zilde görünüyor."

**"Neden basit bir if yerine Strategy?"**
> "Yeni bir kanal geldiğinde mevcut kodu açmıyorum; yeni bir sınıf ve bir kayıt satırı yetiyor. Bu Open/Closed. Rakamlar config'de; onlar için kod hiç değişmiyor."

**"Kullanıcı fiyatı tarayıcıda değiştirebilir mi?"**
> "Hayır. Sepette fiyat yok; sunucu her seferinde veritabanından yeniden hesaplıyor. Sepette görülen tutar ile kaydedilen tutar aynı metottan geçiyor."

**"Sipariş yarıda kalırsa ne olur?"**
> "Sipariş, kalemleri ve bildirimleri tek transaction'da yazılıyor (Unit of Work); ya hepsi kaydedilir ya hiçbiri. Doğrulama hata verirse veritabanına hiçbir şey gitmiyor."

**"Teslim edilmiş bir sipariş iptal edilebilir mi?"**
> "Hayır. `DeliveredState` hiçbir geçişe izin vermiyor. Hem servis reddediyor hem sayfada düğme çıkmıyor."

**"DI'ı neden kullandın?"**
> "Sınıflar birbirini `new`'lemiyor, arayüz istiyor. Yeni bir kural eklerken servise dokunmuyorum ve testte gerçek veritabanı yerine sahte nesne verebiliyorum."

**"Decorator yerine basit bir toplama neden yetmez?"**
> "Bugün bütün seçenekler sabit tutar ekliyor, toplama yeterdi. Ama yüzdelik bir seçenek ya da üst sınırlı bir indirim gelirse Decorator ile sadece yeni bir sınıf yazıyorum, hesap kodu değişmiyor."

**"Chain'de bir halka hata verince ne oluyor?"**
> "`BusinessException` fırlıyor, sonraki halkalar ve kayıt çalışmıyor. Controller exception'ı yakalayıp sepet sayfasını hata mesajıyla, yazılanlar kaybolmadan tekrar gösteriyor."

**"State'i klasik GoF State'e nasıl çevirirdin?"**
> "Davranışı durum sınıflarına taşırdım: örneğin `OnEnterAsync` metodu ekler, `ReadyState`'te müşteriye 'siparişiniz hazır' bildirimi gönderirdim. Şu an durum sınıfları sadece izinli geçişleri söylüyor."

---

## 15. Nasıl çalışmalısın?

1. **Akışı canlı izle:** Visual Studio'da `OrderService.PlaceOrderAsync` başına breakpoint koy, sitede bir sipariş ver ve F11 ile her satırın içine gir. Bölüm 6'daki desenleri sırayla göreceksin.
2. **Kural boz:** Adressiz bir teslimat siparişi ver ve hangi halkanın durdurduğunu izle. Hazırlanıyor durumundaki bir siparişi Teslim Edildi yapmayı dene.
3. **Genişlet:** Gece 23:00–07:00 teslimatı reddeden bir halka yaz. Yeni sınıf + `BusinessServiceRegistration`'a bir satır yetiyorsa yapıyı anlamışsın demektir.
4. **Ayrıntıyı oku:** `docs/kod-rehberi/` sırayla: 01 (DI ve Program.cs), 02 (veritabanı ve Unit of Work), 04 (servisler), 05 (desenler, satır satır).
