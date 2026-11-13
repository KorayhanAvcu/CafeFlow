# 03 · DTO'lar ve Mapster

> Entity veritabanını temsil eder, DTO ise ekranla servis arasında taşınan veriyi. Bu bölüm neden ikisini ayırdığımızı ve **Mapster**'ın aradaki kopyalamayı nasıl yaptığını anlatıyor.
>
> Proje önce AutoMapper 13 kullanıyordu. AutoMapper'ın bu sürümünde bir güvenlik açığı çıktı (GHSA-rvv3-g6hj-g44x), açığı kapatan sürümler ise ücretli lisansa geçti. Bu yüzden ücretsiz ve açık kaynak (MIT) **Mapster 10**'a geçildi. Kullanım neredeyse aynı kaldı: servisler yine `IMapper` alıp `_mapper.Map<T>(...)` çağırıyor; değişen sadece eşleme dosyasının yazılışı (bölüm 4).

---

## 1. DTO neden var?

Controller doğrudan `Order` entity'si alıp döndürseydi ne olurdu?

1. **Over-posting (fazla veri gönderme) saldırısı:** Sipariş formu `Order` entity'sine bind edilseydi, kötü niyetli biri forma `Total=1` ya da `Status=Delivered` alanı ekleyebilirdi. `CreateOrderDto`'da bu alanlar **yok**, dolayısıyla gönderilseler bile bir yere yazılmazlar.
2. **Gereksiz veri ve döngüler:** `Order → Customer → Orders → Customer...` gibi navigation zincirleri JSON'a çevrilirken sonsuz döngüye girer. DTO düz ve sınırlıdır.
3. **Ekranın ihtiyacı farklı:** Admin listesi `CustomerFullName` ister; entity'de bu bilgi `order.Customer.FullName` olarak iç içe durur. DTO'da ise düz bir alan.
4. **Katman bağımsızlığı:** Veritabanına bir kolon eklemek ekranı etkilemez.

## 2. İsimlendirme sözleşmesi

| Ek | Anlam | Örnek |
|---|---|---|
| `ResultXDto` | Okuma: ekrana giden veri | `ResultProductDto` (CategoryName ile) |
| `CreateXDto` | Yeni kayıt formu | `CreateCategoryDto`: Id yok, Code/Slug yok (sunucu üretir) |
| `UpdateXDto` | Düzenleme formu | `UpdateCategoryDto`: Id var |
| `CartItemDto` | Session'daki sepet satırı | ProductId + OptionIds + Quantity |
| `CreateOrderDto` | Sepet sayfasındaki sipariş formu | ad, telefon, adres... |

### "Tek form" hilesi: UpdateXDto ile hem ekleme hem düzenleme
Admin kategori formu her zaman `UpdateCategoryDto` ile açılıyor:
- `/admin/categories/new` → `new UpdateCategoryDto()` → `Id = 0`
- `/admin/categories/3/edit` → `Id = 3`

Controller formu olduğu gibi `CategoryService.SaveAsync`'e veriyor; servis `Id == 0` ise DTO'yu Mapster ile `CreateCategoryDto`'ya çevirip `CreateAsync`'i çağırıyor, değilse `UpdateAsync`'i. Boş ve dolu formu da servis hazırlıyor (`GetFormAsync`). Böylece **tek bir `Form.cshtml`** iki işi görüyor. Formda gizli bir `<input name="Id">` alanı var.

## 3. DTO'ların içeriği (dikkat edilecek alanlar)

### `CartItemDto`: fiyat yok!
```csharp
public int ProductId { get; set; }
public List<int> OptionIds { get; set; } = new List<int>();
public int Quantity { get; set; }
```
Sepet Session'da JSON olarak duruyor: `[{"ProductId":2,"OptionIds":[3,5,7],"Quantity":2}]`. Fiyat tutulmuyor, çünkü:
1. Güvenlik: Session sunucuda olsa bile "fiyat sadece veritabanından gelir" ilkesi basit ve kesin.
2. Güncellik: Admin fiyatı değiştirirse sepet anında yeni fiyatı gösterir.

### `CreateOrderDto`
```csharp
public OrderChannel Channel { get; set; }   // formdan DEĞİL, controller Session'dan doldurur
public string? FullName, Phone, Address, AddressNotes, Note;
public DateTime? PickupTime;
```
Hepsi nullable: masa siparişinde ad ve telefon gelmez. Hangi alanın zorunlu olduğuna DTO'daki attribute'lar değil, **Chain of Responsibility** karar veriyor (`ChannelInfoHandler`). Çünkü kural kanala göre değişiyor ve sabit bir `[Required]` bunu ifade edemez.

### `ResultOrderDto` ve flattening
```csharp
public string? CustomerFullName { get; set; }   // ← order.Customer.FullName
public string? CustomerPhone { get; set; }      // ← order.Customer.Phone
public int? TableNumber { get; set; }           // ← order.Table.Number
public string? TableArea { get; set; }          // ← order.Table.Area
public List<ResultOrderDetailDto> Details ...
public List<ResultOrderStatusHistoryDto> StatusHistories ...
```
Bu alanlar için mapping'de hiçbir satır yazılmamış. Mapster'ın **flattening** özelliği isimden çözüyor: `CustomerFullName` → "Customer" property'si var mı? Var. Onun içinde "FullName" var mı? Var → `src.Customer.FullName`. Customer null ise (masa siparişi) hata vermez, `null` yazar. (Bunu testler de doğruluyor: masa siparişleri admin listesinde sorunsuz görünüyor.)

### `ResultOrderDetailDto`
```csharp
public string? ProductImageUrl { get; set; }       // ← detail.Product.ImageUrl
public string? ProductCategoryName { get; set; }   // ← detail.Product.Category.Name (iki seviye!)
public string? ProductCategorySlug { get; set; }
```
DTO'daki yorum önemli: *"sadece sepette dolu gelir"*. Neden?
- Sepet hesaplanırken (`CalculateOrderAsync`) `detail.Product = product` atanıyor ve ürün `Category` ile Include edilmiş. Flattening bu yüzden çalışıyor ve sepet sayfası resmi gösterebiliyor.
- Kayıtlı siparişi okurken (`OrdersWithDetails()`) `Details.Product` Include **edilmiyor**. Bu alanlar null gelir. Admin sipariş detayında resim yok, sadece kopyalanmış `ProductName` var.

### `ResultCustomerDto`: hesaplanan alanlar
```csharp
public int OrderCount; public decimal TotalSpent; public DateTime? LastOrderDate;
```
Bunlar bir tabloda yok; mapping sırasında hesaplanıyor.

## 4. `GeneralMapping`: Mapster eşlemeleri

```csharp
public class GeneralMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Kaynak, Hedef>()
            .Map(dest => dest.Alan, src => hesap);
    }
}
```
- `IRegister` arayüzünü uygulayan sınıfın `Register` metodunda kurallar yazılır.
- **Sadece özel kuralı olan eşlemeler yazılır.** AutoMapper'da her eşleme için `CreateMap` yazmak gerekiyordu; Mapster ise kuralı olmayan eşlemeyi (ör. `Product → ResultProductDto`) ilk kullanımda isimlerden kendisi çıkarıyor. Bu yüzden dosya 23 satırdan 5 kurala indi.

### Program.cs'teki kayıt
```csharp
TypeAdapterConfig mapsterConfig = TypeAdapterConfig.GlobalSettings;
mapsterConfig.Scan(typeof(GeneralMapping).Assembly);   // IRegister sınıflarını bulup Register'ı çağırır
builder.Services.AddSingleton(mapsterConfig);
builder.Services.AddScoped<IMapper, ServiceMapper>();   // servislere IMapper verilir
```
`IMapper` ve `ServiceMapper`, `Mapster.DependencyInjection` paketinden (`using MapsterMapper;`). Servislerin kodu AutoMapper'dakiyle aynı: constructor'da `IMapper mapper` alıp `_mapper.Map<ResultCategoryDto>(...)` çağırıyorlar.

### Kategori
```csharp
config.NewConfig<Category, ResultCategoryDto>()
    .Map(dest => dest.ProductCount, src => src.Products.Count);
```
`ProductCount` adı hiçbir alanla eşleşmiyor (flattening `Product.Count` diye bir şey arar ve bulamaz). Bu yüzden `.Map(...)` ile elle yazılmış. (AutoMapper'daki karşılığı `ForMember(d => d.ProductCount, o => o.MapFrom(s => s.Products.Count))` idi.)

**Tuzak:** `src.Products.Count` sadece Products **Include edildiyse** doğru olur. `CategoryService.GetAllAsync` `.Include(c => c.Products)` yapıyor → doğru. Kategoriyi `FindAsync` ile (Include olmadan) yükleyip `ResultCategoryDto`'ya çevirseydik `ProductCount` 0 gelirdi; bunu bilmeden kullanan biri yanlış sonuç alırdı.

`GetActiveAsync`'te `.Include(c => c.Products.Where(p => p.IsActive))` var. Bu **filtered include** (EF Core 5+) ile `ProductCount` aktif ürün sayısı olur. Ana sayfadaki kategori kartları "ürünü olmayan kategoriyi gösterme" kararını bu sayıya göre veriyor.

Kuralsız eşlemeler (yazılmadı, Mapster kendisi çıkarıyor):
```text
CreateCategoryDto → Category, UpdateCategoryDto → Category, Category → UpdateCategoryDto
UpdateCategoryDto → CreateCategoryDto   (Id = 0 ise yeni kayıt)
```
DTO'dan DTO'ya eşleme de serbest; Mapster herhangi iki sınıf için çalışır.

### `_mapper.Map(dto, entity)`: var olan nesnenin üstüne yazmak
Servislerde iki farklı kullanım göreceksin:
```csharp
Category category = _mapper.Map<Category>(dto);   // YENİ nesne oluşturur
_mapper.Map(dto, category);                        // VAR OLAN nesnenin alanlarını günceller
```
İkincisi update'te kullanılıyor: veritabanından gelen `category` (EF tarafından izleniyor) üzerine DTO'daki alanlar yazılıyor. DTO'da olmayan alanlar (`Code`, `Slug`, `CreatedAt`) **dokunulmadan** kalıyor. EF Core değişen alanları fark ediyor.

### Masa
```csharp
config.NewConfig<Table, ResultTableDto>()
    .Map(dest => dest.OrderCount, src => src.Orders.Count);
```
`Product → ResultProductDto` (CategoryName, CategorySlug, CategoryIcon flattening ile) ve `ProductOption → ResultProductOptionDto` için kural yazılmadı.

### Müşteri: LINQ içeren mapping
```csharp
config.NewConfig<Customer, ResultCustomerDto>()
    .Map(d => d.OrderCount,    s => s.Orders.Count)
    .Map(d => d.TotalSpent,    s => s.Orders.Where(x => x.Status != OrderStatus.Cancelled).Sum(x => x.Total))
    .Map(d => d.LastOrderDate, s => s.Orders.Count == 0 ? (DateTime?)null : s.Orders.Max(x => x.CreatedAt));
```
- İptal edilen siparişler harcamaya dahil değil.
- `Max` boş listede exception fırlatır; bu yüzden önce `Count == 0` kontrolü yapılıyor.
- `(DateTime?)null`: ternary'nin iki tarafının da aynı tipte olması gerekiyor (`DateTime?`).
- Bu hesap **RAM'de** yapılıyor: `CustomerService` önce `Include(c => c.Orders)` ile bütün siparişleri çekiyor, Mapster sonra LINQ to Objects ile topluyor. Az veride sorun değil; çok veride Mapster'ın `ProjectToType<T>()` metoduyla hesabı SQL'e yaptırmak gerekir.

### Bildirim
```csharp
config.NewConfig<Notification, ResultNotificationDto>()
    .Map(d => d.OrderNo, s => s.Order != null ? s.Order.OrderNo : null);
```
Bildirimin siparişi silinmiş olabilir; o zaman sipariş numarası boş kalır. Kural açıkça yazıldı ki bu durum okuyana görünsün.

### Sipariş
```text
Order → ResultOrderDto, OrderDetail → ResultOrderDetailDto, OrderDetailOption → ResultOrderDetailOptionDto,
OrderStatusHistory → ResultOrderStatusHistoryDto, CreateOrderDto → Order     (hepsi kuralsız)
```
- İç içe listeler (`Details`, `Options`, `StatusHistories`) için ayrıca bir şey yazmak gerekmiyor. Mapster eleman tiplerini de isimden eşleyip listeleri çeviriyor.
- `CreateOrderDto → Order`: `Channel`, `Address`, `AddressNotes`, `PickupTime` ve `Note` aynı isimde, otomatik kopyalanıyor. `FullName` ve `Phone` ise `Order`'da yok; Mapster bunları sessizce **yok sayıyor**. Müşteri bilgisi `OrderService`'te ayrıca işleniyor.

## 5. Neden AutoMapper'dan Mapster'a geçildi?
Eski derlemelerde şu uyarı çıkıyordu:
```text
warning NU1903: 'AutoMapper' 13.0.1 ... yüksek önem dereceli bilinen bir güvenlik açığı
```
- Açık: çok derin iç içe nesneler eşlenirken AutoMapper derinlik sınırı koymadan kendini çağırıyor; saldırgan özel bir nesne grafiği verirse `StackOverflowException` ile uygulama çöküyor (DoS).
- Açığı kapatan sürümler (15.1.1 ve sonrası) **ücretli lisansa** geçti; son ücretsiz (MIT) sürüm 14.0.0 ise hâlâ açıklı.
- Mapster 10.0.14: MIT lisanslı, ücretsiz, .NET 8 destekli, bilinen açığı yok. `dotnet list package --vulnerable --include-transitive` artık hiçbir projede açık göstermiyor ve derleme **0 uyarı** veriyor.
- Değişiklik küçük oldu: servislerde sadece `using AutoMapper;` → `using MapsterMapper;`; `GeneralMapping` yeniden yazıldı; Program.cs'teki kayıt değişti. Bütün uçtan uca testler (170) değişiklikten sonra da geçti.

---

## Kendini sına
1. Sipariş formunu doğrudan `Order` entity'sine bind etseydik saldırgan neyi değiştirebilirdi?
2. `ResultOrderDto.TableNumber` mapping'de hiç yazılmadığı halde nasıl doluyor?
3. Admin sipariş detayında `ProductImageUrl` neden null?
4. `_mapper.Map<Category>(dto)` ile `_mapper.Map(dto, category)` farkı nedir? Update'te neden ikincisi kullanılıyor?
5. Kategori Include olmadan yüklenip DTO'ya çevrilirse `ProductCount` neden güvenilir olmaz?
6. AutoMapper'da 23 `CreateMap` vardı; Mapster'da neden sadece 5 kural yazıldı?
