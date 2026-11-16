# 03 · DTO'lar ve AutoMapper

> Entity veritabanını temsil eder, DTO ise ekranla servis arasında taşınan veriyi. Bu bölüm neden ikisini ayırdığımızı ve **AutoMapper**'ın aradaki kopyalamayı nasıl yaptığını anlatıyor.
>
> Projede **AutoMapper 12.0.1** kullanılıyor; eşlemeler tablo başına bir `Profile` sınıfında (`CategoryMappings`, `ProductMappings` ...). Sürüm ve güvenlik uyarıları için bölüm 5'e bak.

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
Bu alanlar için `CreateMap<Order, ResultOrderDto>()` dışında hiçbir kural yazılmamış. AutoMapper'ın **flattening** özelliği isimden çözüyor: `CustomerFullName` → "Customer" property'si var mı? Var. Onun içinde "FullName" var mı? Var → `src.Customer.FullName`. Customer null ise (masa siparişi) hata vermez, `null` yazar. (Bunu testler de doğruluyor: masa siparişleri admin listesinde sorunsuz görünüyor.)

### `ResultOrderDetailDto`
```csharp
public string? ProductImageUrl { get; set; }       // ← detail.Product.ImageUrl
public string? ProductCategoryName { get; set; }   // ← detail.Product.Category.Name (iki seviye!)
public string? ProductCategorySlug { get; set; }
```
DTO'daki yorum önemli: *"sadece sepette dolu gelir"*. Neden?
- Sepet hesaplanırken (`OrderPricingService.ApplyPricesAsync`) `detail.Product = product` atanıyor ve ürün `Category` ile Include edilmiş. Flattening bu yüzden çalışıyor ve sepet sayfası resmi gösterebiliyor.
- Kayıtlı siparişi okurken (`OrderRepository.WithDetails()`) `Details.Product` Include **edilmiyor**. Bu alanlar null gelir. Admin sipariş detayında resim yok, sadece kopyalanmış `ProductName` var.

### `ResultCustomerDto`: hesaplanan alanlar
```csharp
public int OrderCount; public decimal TotalSpent; public DateTime? LastOrderDate;
```
Bunlar bir tabloda yok; mapping sırasında hesaplanıyor.

## 4. AutoMapper eşlemeleri: `Profile` sınıfları

Eşlemeler `BusinessLayer/Mapping/` klasöründe, **tablo başına bir dosya** olarak duruyor:

| Dosya | İçindeki eşlemeler |
|---|---|
| `CategoryMappings.cs` | Create/Update DTO ↔ Category, Category → ResultCategoryDto |
| `ProductMappings.cs` | Create/Update DTO ↔ Product, Product → ResultProductDto, ProductOption → ResultProductOptionDto |
| `TableMappings.cs` | Create/Update DTO ↔ Table, Table → ResultTableDto |
| `CustomerMappings.cs` | Customer → ResultCustomerDto |
| `NotificationMappings.cs` | Notification → ResultNotificationDto |
| `OrderMappings.cs` | CreateOrderDto → Order, Order → ResultOrderDto ve iç içe kalemler |

Hepsi aynı kalıpta:
```csharp
public class CategoryMappings : Profile          // AutoMapper'ın "kural sınıfı"
{
    public CategoryMappings()                    // kurallar CONSTRUCTOR'da yazılır
    {
        CreateMap<Kaynak, Hedef>();              // kaynak → hedef çevirisine izin ver
    }
}
```

### `CreateMap` ne yapar?
`CreateMap<A, B>()` AutoMapper'a **"A'yı B'ye çevirmeye izin var"** der. Aynı isimli alanlar kendiliğinden kopyalanır (`Name → Name`). **AutoMapper'da her çeviri için bir `CreateMap` yazılmak zorunda.** Yazılmazsa derleme hata vermez, ama o satır çalıştığında `AutoMapperMappingException: Missing type map configuration` fırlar. Bu yüzden projedeki her `_mapper.Map` çağrısının tip çifti bir Profile'da var.

### Kategori (`CategoryMappings.cs`)
```csharp
CreateMap<CreateCategoryDto, Category>();                    // yeni kayıt: form → entity
CreateMap<UpdateCategoryDto, Category>().ReverseMap();       // düzenleme: form → entity, ReverseMap: entity → form
CreateMap<UpdateCategoryDto, CreateCategoryDto>();           // kaydet düğmesi: Id = 0 ise yeni kayıt
CreateMap<Category, ResultCategoryDto>()
    .ForMember(dest => dest.ProductCount, opt => opt.MapFrom(src => src.Products.Count));
```
- **`ReverseMap()`**: tersini de oluşturur. `UpdateCategoryDto → Category` yazıp `.ReverseMap()` deyince `Category → UpdateCategoryDto` da tanımlanmış olur. Düzenleme sayfası açılırken entity forma bu ters eşlemeyle çevriliyor.
- **`ForMember(hedef, opt => opt.MapFrom(kaynak))`**: "hedefteki şu alanı kaynaktan şöyle hesapla." `ProductCount` adı hiçbir alanla eşleşmiyor, bu yüzden elle yazıldı.

**Tuzak:** `src.Products.Count` sadece Products **Include edildiyse** doğru olur. `CategoryService.GetAllAsync` → `CategoryRepository.GetAllWithProductsAsync` `.Include(c => c.Products)` yapıyor → doğru. Kategoriyi `FindAsync` ile (Include olmadan) yükleyip `ResultCategoryDto`'ya çevirseydik `ProductCount` 0 gelirdi; hata vermez, sessizce yanlış olur.

`GetActiveAsync`'in kullandığı `GetActiveWithActiveProductsAsync`'te `.Include(c => c.Products.Where(p => p.IsActive))` var. Bu **filtered include** (EF Core 5+) ile `ProductCount` aktif ürün sayısı olur. Ana sayfadaki kategori kartları "ürünü olmayan kategoriyi gösterme" kararını bu sayıya göre veriyor.

### Ürün (`ProductMappings.cs`): flattening
```csharp
CreateMap<CreateProductDto, Product>();
CreateMap<UpdateProductDto, Product>().ReverseMap();
CreateMap<UpdateProductDto, CreateProductDto>();
CreateMap<Product, ResultProductDto>();          // CategoryName, CategorySlug, CategoryIcon için kural YOK
CreateMap<ProductOption, ResultProductOptionDto>();
```
`ResultProductDto.CategoryName` için hiçbir kural yazılmadı. AutoMapper'ın **flattening** özelliği isimden çözüyor: `CategoryName` → kaynakta "Category" property'si var mı? Var. Onun içinde "Name" var mı? Var → `src.Category.Name`. Category null ise hata vermez, alan boş kalır.

### Masa (`TableMappings.cs`)
```csharp
CreateMap<CreateTableDto, Table>();
CreateMap<UpdateTableDto, Table>().ReverseMap();   // QrToken DTO'da yok → düzenlemede korunur
CreateMap<UpdateTableDto, CreateTableDto>();
CreateMap<Table, ResultTableDto>()
    .ForMember(dest => dest.OrderCount, opt => opt.MapFrom(src => src.Orders.Count));
```

### Müşteri (`CustomerMappings.cs`): LINQ içeren eşleme
```csharp
CreateMap<Customer, ResultCustomerDto>()
    .ForMember(dest => dest.OrderCount, opt => opt.MapFrom(src => src.Orders.Count))
    .ForMember(dest => dest.TotalSpent, opt => opt.MapFrom(src =>
        src.Orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.Total)))
    .ForMember(dest => dest.LastOrderDate, opt => opt.MapFrom(src =>
        src.Orders.Count == 0 ? (DateTime?)null : src.Orders.Max(o => o.CreatedAt)));
```
- İptal edilen siparişler harcamaya dahil değil.
- `Max` boş listede exception fırlatır; bu yüzden önce `Count == 0` kontrolü yapılıyor.
- `(DateTime?)null`: ternary'nin iki tarafının da aynı tipte olması gerekiyor (`DateTime?`).
- Bu hesap **RAM'de** yapılıyor: repository önce `Include(c => c.Orders)` ile siparişleri çekiyor, AutoMapper sonra LINQ to Objects ile topluyor. Çok veride AutoMapper'ın `ProjectTo<T>()` metoduyla hesabı SQL'e yaptırmak gerekir.

### Bildirim (`NotificationMappings.cs`)
```csharp
CreateMap<Notification, ResultNotificationDto>()
    .ForMember(dest => dest.OrderNo, opt => opt.MapFrom(src => src.Order != null ? src.Order.OrderNo : null));
```
Bildirimin siparişi silinmiş olabilir; o zaman sipariş numarası boş kalır. Kural açıkça yazıldı ki bu durum okuyana görünsün.

### Sipariş (`OrderMappings.cs`): iç içe listeler
```csharp
CreateMap<CreateOrderDto, Order>();
CreateMap<Order, ResultOrderDto>();
CreateMap<OrderDetail, ResultOrderDetailDto>();
CreateMap<OrderDetailOption, ResultOrderDetailOptionDto>();
CreateMap<OrderStatusHistory, ResultOrderStatusHistoryDto>();
```
- `Order → ResultOrderDto` çevrilirken içindeki `Details` listesi de çevriliyor. Bunun için **eleman tiplerinin** (`OrderDetail → ResultOrderDetailDto`, `OrderDetailOption → ...`, `OrderStatusHistory → ...`) de `CreateMap` ile tanımlanmış olması gerekiyor. Listeyi (`List<A> → List<B>`) ise AutoMapper kendisi hallediyor.
- `CreateOrderDto → Order`: `Channel`, `Address`, `AddressNotes`, `PickupTime` ve `Note` aynı isimde, otomatik kopyalanıyor. `FullName` ve `Phone` `Order`'da yok; AutoMapper **kaynaktaki fazla alanları yok sayar**. Müşteri bilgisi `OrderService`'te ayrıca işleniyor.
- `ResultOrderDto.AllowedNextStatuses` kaynakta yok; boş kalıyor ve `OrderQueryService` State'e sorup dolduruyor.

### DI kaydı (`BusinessServiceRegistration`)
```csharp
services.AddAutoMapper(typeof(CategoryMappings).Assembly);
```
Tek satır: "`CategoryMappings`'in bulunduğu projedeki (BusinessLayer) **bütün `Profile` sınıflarını** bul." Uygulama açılırken her Profile'ın constructor'ı bir kez çalışır ve `CreateMap` kuralları kaydedilir. `IMapper` DI'a eklenir. Yarın `PaymentMappings : Profile` diye yeni bir dosya açarsan o da otomatik bulunur; kayıt satırına dokunmazsın. (`AddAutoMapper` metodu `AutoMapper.Extensions.Microsoft.DependencyInjection` paketinden geliyor.)

### Servislerde kullanım: `Map<T>` ile `Map(src, dest)` farkı
```csharp
using AutoMapper;

public CategoryService(..., IMapper mapper) { _mapper = mapper; }   // DI'dan gelir

return _mapper.Map<List<ResultCategoryDto>>(categories);   // 1) okuma: entity → YENİ DTO (liste de olur)
Category category = _mapper.Map<Category>(dto);             // 2) ekleme: DTO → YENİ entity
_mapper.Map(dto, category);                                  // 3) güncelleme: DTO → VAR OLAN entity'nin üstüne
```

| Yazım | Ne yapar | Ne zaman |
|---|---|---|
| `_mapper.Map<Hedef>(kaynak)` | **Yeni** bir nesne oluşturur | Okuma ve ekleme |
| `_mapper.Map(kaynak, hedef)` | **Var olan** nesnenin üstüne yazar | Güncelleme |

Güncellemede neden ikincisi? Veritabanından gelen `category` (EF tarafından izleniyor) üzerinde `Code`, `Slug`, `CreatedAt` gibi alanlar var ama DTO'da yok. Yeni nesne oluşturulsaydı bunlar boş kalırdı. Üstüne yazınca sadece DTO'daki alanlar değişiyor, diğerleri **dokunulmadan** kalıyor; EF Core da sadece değişen kolonları UPDATE ediyor.

### Uçtan uca: ne zaman ne oluyor?
```text
Uygulama açılırken (bir kez):
  AddAutoMapper → BusinessLayer'daki 6 Profile bulunur → constructor'lar çalışır → CreateMap kuralları kaydedilir

Admin kategori listesini açınca:
  CategoryService.GetAllAsync
    → repository: kategoriler + ürünleri (Include)
    → _mapper.Map<List<ResultCategoryDto>>(categories)
         Id, Name, Slug ...    → aynı isim, kendiliğinden kopyalanır
         ProductCount          → ForMember kuralı: Products.Count
    → sayfaya DTO listesi gider
```

## 5. Paket sürümü: AutoMapper 12.0.1

```xml
<PackageReference Include="AutoMapper" Version="12.0.1" />
<PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.1" />
```
- Eğitimdeki diğer projelerde kullanılan sürümle aynı tutuldu; Profile yazımı da aynı (`CreateMap`, `ReverseMap`, `ForMember`).
- **Bilinmesi gereken:** NuGet bu sürüm için derlemede **3 yüksek önem dereceli güvenlik uyarısı** veriyor (`NU1903`: GHSA-rvv3-g6hj-g44x, GHSA-6vhm-j9h7-f9gh, GHSA-gpwp-5797-mjff). Örneğin biri, çok derin iç içe nesneler eşlenirken derinlik sınırı olmadığı için uygulamanın `StackOverflowException` ile çökebilmesi (DoS). Bu projede eşlenen nesneler kullanıcıdan gelen serbest JSON değil, sabit yapılı DTO ve entity'ler; yine de derleme çıktısında bu uyarılar görünür.
- AutoMapper'ın açıkları kapatan yeni sürümleri **ticari lisansa** geçti (lisans anahtarı istiyor). Ücretsiz ve açıksız bir alternatif istenirse **Mapster** (MIT) kullanılabilir; projede daha önce Mapster vardı ve servis kodu iki kütüphanede de aynı (`IMapper`, `_mapper.Map<T>(...)`). Değişen sadece eşleme dosyaları ve DI kaydı.

### AutoMapper ile Mapster karşılaştırması
| | AutoMapper | Mapster |
|---|---|---|
| Kural sınıfı | `Profile` (kurallar constructor'da) | `IRegister` (kurallar `Register` metodunda) |
| Kural başlatma | `CreateMap<A, B>()` | `config.NewConfig<A, B>()` |
| Özel alan | `.ForMember(d => d.X, o => o.MapFrom(s => ...))` | `.Map(d => d.X, s => ...)` |
| Kuralsız eşleme | **Her biri için `CreateMap` yazılmalı** | Yazılmaz, isimlerden kendisi çıkarır |
| Ters eşleme | `.ReverseMap()` | `.TwoWays()` |
| DI kaydı | `services.AddAutoMapper(assembly)` | `TypeAdapterConfig.Scan(...)` + `ServiceMapper` |
| Servisteki kullanım | `_mapper.Map<T>(x)`, `_mapper.Map(x, y)` | **Aynı** |
| Lisans | 12.x MIT (açıklı); yeni sürümler ticari | MIT, ücretsiz |

---

## Kendini sına
1. Sipariş formunu doğrudan `Order` entity'sine bind etseydik saldırgan neyi değiştirebilirdi?
2. `ResultOrderDto.TableNumber` için hiçbir kural yazılmadığı halde nasıl doluyor?
3. Admin sipariş detayında `ProductImageUrl` neden null?
4. `_mapper.Map<Category>(dto)` ile `_mapper.Map(dto, category)` farkı nedir? Update'te neden ikincisi kullanılıyor?
5. Kategori Include olmadan yüklenip DTO'ya çevrilirse `ProductCount` neden güvenilir olmaz?
6. `OrderMappings`'ten `CreateMap<OrderDetail, ResultOrderDetailDto>()` satırını silsek derleme hata verir mi? Ne zaman, hangi hatayı görürdük?
7. `ReverseMap()` ne işe yarıyor? Projede nerede kullanılıyor?
