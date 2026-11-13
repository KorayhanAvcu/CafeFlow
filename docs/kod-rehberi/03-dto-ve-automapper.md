# 03 · DTO'lar ve AutoMapper

> Entity veritabanını temsil eder, DTO ise ekranla servis arasında taşınan veriyi. Bu bölüm neden ikisini ayırdığımızı ve AutoMapper'ın aradaki kopyalamayı nasıl yaptığını anlatıyor.

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

Controller formu olduğu gibi `CategoryService.SaveAsync`'e veriyor; servis `Id == 0` ise DTO'yu AutoMapper ile `CreateCategoryDto`'ya çevirip `CreateAsync`'i çağırıyor, değilse `UpdateAsync`'i. Boş ve dolu formu da servis hazırlıyor (`GetFormAsync`). Böylece **tek bir `Form.cshtml`** iki işi görüyor. Formda gizli bir `<input name="Id">` alanı var.

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
Bu alanlar için mapping'de hiçbir satır yazılmamış. AutoMapper'ın **flattening** özelliği isimden çözüyor: `CustomerFullName` → "Customer" property'si var mı? Var. Onun içinde "FullName" var mı? Var → `src.Customer.FullName`. Customer null ise (masa siparişi) hata vermez, `null` yazar.

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

## 4. `GeneralMapping`: bütün eşlemeler

```csharp
public class GeneralMapping : Profile
{
    public GeneralMapping()
    {
        CreateMap<Kaynak, Hedef>();
        ...
    }
}
```
`Profile`'dan türeyen sınıfın constructor'ında eşlemeler tanımlanır. `AddAutoMapper(typeof(GeneralMapping))` bu sınıfı bulur.

### Kategori
```csharp
CreateMap<Category, ResultCategoryDto>()
    .ForMember(dest => dest.ProductCount, opt => opt.MapFrom(src => src.Products.Count));
```
`ProductCount` adı hiçbir alanla eşleşmiyor (flattening `Product.Count` diye bir şey arar ve bulamaz). Bu yüzden `ForMember` ile elle yazılmış.

**Tuzak:** `src.Products.Count` sadece Products **Include edildiyse** doğru olur. `CategoryService.GetAllAsync` `.Include(c => c.Products)` yapıyor → doğru. Kategoriyi `FindAsync` ile (Include olmadan) yükleyip `ResultCategoryDto`'ya çevirseydik `ProductCount` 0 gelirdi; bunu bilmeden kullanan biri yanlış sonuç alırdı.

`GetActiveAsync`'te `.Include(c => c.Products.Where(p => p.IsActive))` var. Bu **filtered include** (EF Core 5+) ile `ProductCount` aktif ürün sayısı olur. `HomeController` "ürünü olmayan kategoriyi gösterme" kararını bu sayıya göre veriyor.

```csharp
CreateMap<CreateCategoryDto, Category>();
CreateMap<UpdateCategoryDto, Category>();
CreateMap<ResultCategoryDto, UpdateCategoryDto>();   // düzenleme formunu doldurmak için
CreateMap<UpdateCategoryDto, CreateCategoryDto>();   // Id = 0 ise yeni kayıt
```
DTO'dan DTO'ya eşleme de serbest. AutoMapper sadece entity için değil, herhangi iki sınıf için çalışır.

### `_mapper.Map(dto, entity)`: var olan nesnenin üstüne yazmak
Servislerde iki farklı kullanım göreceksin:
```csharp
Category category = _mapper.Map<Category>(dto);   // YENİ nesne oluşturur
_mapper.Map(dto, category);                        // VAR OLAN nesnenin alanlarını günceller
```
İkincisi update'te kullanılıyor: veritabanından gelen `category` (EF tarafından izleniyor) üzerine DTO'daki alanlar yazılıyor. DTO'da olmayan alanlar (`Code`, `Slug`, `CreatedAt`) **dokunulmadan** kalıyor. EF Core değişen alanları fark ediyor.

### Ürün, masa ve seçenekler
```csharp
CreateMap<Product, ResultProductDto>();                // CategoryName, CategorySlug, CategoryIcon flattening ile
CreateMap<Table, ResultTableDto>()
    .ForMember(dest => dest.OrderCount, opt => opt.MapFrom(src => src.Orders.Count));
CreateMap<ProductOption, ResultProductOptionDto>();
```

### Müşteri: LINQ içeren mapping
```csharp
CreateMap<Customer, ResultCustomerDto>()
    .ForMember(d => d.OrderCount,    o => o.MapFrom(s => s.Orders.Count))
    .ForMember(d => d.TotalSpent,    o => o.MapFrom(s => s.Orders.Where(x => x.Status != OrderStatus.Cancelled).Sum(x => x.Total)))
    .ForMember(d => d.LastOrderDate, o => o.MapFrom(s => s.Orders.Count == 0 ? (DateTime?)null : s.Orders.Max(x => x.CreatedAt)));
```
- İptal edilen siparişler harcamaya dahil değil.
- `Max` boş listede exception fırlatır; bu yüzden önce `Count == 0` kontrolü yapılıyor.
- `(DateTime?)null`: ternary'nin iki tarafının da aynı tipte olması gerekiyor (`DateTime?`).
- Bu hesap **RAM'de** yapılıyor: `CustomerService` önce `Include(c => c.Orders)` ile bütün siparişleri çekiyor, AutoMapper sonra LINQ to Objects ile topluyor. Az veride sorun değil; çok veride `ProjectTo` ile SQL'e yaptırmak gerekir.

### Bildirim
```csharp
CreateMap<Notification, ResultNotificationDto>()
    .ForMember(d => d.OrderNo, o => o.MapFrom(s => s.Order != null ? s.Order.OrderNo : null));
```
Burada flattening (`OrderOrderNo`) çalışmazdı, çünkü isim `OrderNo`. Elle yazılmış.

### Sipariş
```csharp
CreateMap<Order, ResultOrderDto>();
CreateMap<OrderDetail, ResultOrderDetailDto>();
CreateMap<OrderDetailOption, ResultOrderDetailOptionDto>();
CreateMap<OrderStatusHistory, ResultOrderStatusHistoryDto>();
CreateMap<CreateOrderDto, Order>();
```
- İç içe listeler (`Details`, `Options`, `StatusHistories`) için ayrıca bir şey yazmak gerekmiyor. Eleman tiplerinin eşlemesi tanımlı olduğu için AutoMapper listeleri de çeviriyor.
- `CreateOrderDto → Order`: `Channel`, `Address`, `AddressNotes`, `PickupTime` ve `Note` aynı isimde, otomatik kopyalanıyor. `FullName` ve `Phone` ise `Order`'da yok; AutoMapper bunları sessizce **yok sayıyor** (kaynakta fazla alan olması sorun değil, hedefte eşleşmeyen alan olması AutoMapper'ın yapılandırma doğrulamasında uyarı sebebidir). Müşteri bilgisi `OrderService`'te ayrıca işleniyor.

## 5. AutoMapper ve güvenlik notu
Derlemede çıkan uyarıyı hatırla:
```text
warning NU1903: 'AutoMapper' 13.0.1 ... yüksek önem dereceli bilinen bir güvenlik açığı
```
Açıklar genelde çok derin iç içe nesnelerle yapılan bir DoS (servis dışı bırakma) senaryosu ile ilgilidir. Bu projede kullanıcının kontrol ettiği derin bir nesne grafiği map edilmiyor. Yine de proje teslim edilecekse not düşmekte fayda var. AutoMapper'ın yeni sürümleri ticari lisansa geçti. Alternatif olarak elle mapping yazılabilir veya Mapster gibi bir kütüphane kullanılabilir.

---

## Kendini sına
1. Sipariş formunu doğrudan `Order` entity'sine bind etseydik saldırgan neyi değiştirebilirdi?
2. `ResultOrderDto.TableNumber` mapping'de hiç yazılmadığı halde nasıl doluyor?
3. Admin sipariş detayında `ProductImageUrl` neden null?
4. `_mapper.Map<Category>(dto)` ile `_mapper.Map(dto, category)` farkı nedir? Update'te neden ikincisi kullanılıyor?
5. Kategori Include olmadan yüklenip DTO'ya çevrilirse `ProductCount` neden güvenilir olmaz?
