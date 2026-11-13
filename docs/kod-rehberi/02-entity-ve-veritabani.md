# 02 · Entity'ler, veritabanı, seed ve repository'ler

> Bu bölüm EntityLayer ve DataAccessLayer'ı kapsıyor: tablolar nasıl C# sınıfı olarak yazılmış, EF Core bunları nasıl SQL'e çeviriyor, ilk veri nasıl ekleniyor, repository'ler ne yapıyor.

---

## 1. Enum'lar (`EntityLayer/Enums`)

```csharp
public enum OrderChannel { Delivery, Pickup, Table }
public enum OrderStatus { Received, Preparing, Ready, Delivered, Cancelled }
public enum NotificationTarget { Kitchen, Cashier, Admin }

public static class OptionGroups
{
    public const string Size = "Size";
    public const string Milk = "Milk";
    public const string ExtraShot = "ExtraShot";
    public const string Syrup = "Syrup";
}

public static class AppRoles { public const string Admin = "Admin"; }
```
- Enum'lar C#'ta aslında sayıdır (`Delivery = 0`). Veritabanında ise **metin** olarak saklanıyor (aşağıda `HasConversion<string>()`). SQL'de `SELECT * FROM Orders` dediğinde `0` yerine `Delivery` görürsün. Okunması kolay olur ve enum'un sırası değişse bile eski veri bozulmaz.
- `OptionGroups` bir enum değil, **sabit metinler** sınıfı. Çünkü `ProductOption.GroupKey` bir `string` kolonu. Kodda `"Milk"` yazmak yerine `OptionGroups.Milk` yazınca yazım hatası derleme anında yakalanır.
- `AppRoles.Admin` aynı mantıkla: `[Authorize(Roles = AppRoles.Admin)]`.

## 2. Entity'ler (`EntityLayer/Entities`)

Her entity bir tablo, her property bir kolondur. EF Core **convention** (gelenek) ile çalışır:
- Adı `Id` olan `int` property → primary key + IDENTITY (otomatik artan).
- `XxxId` property'si + `Xxx` navigation property'si → foreign key.
- `List<Y>` → bire-çok ilişkinin "çok" tarafı.

### Category
```csharp
public int Id { get; set; }
public string Code { get; set; }        // CAT-001 (Id'den üretilir)
public string Slug { get; set; }        // "hot", adreste kullanılır: /products?category=hot
public string Name { get; set; }
public string? Description, Icon, ImageUrl;
public int DisplayOrder { get; set; }   // menüdeki sıra
public bool IsActive { get; set; } = true;
public DateTime CreatedAt { get; set; }
public List<Product> Products { get; set; } = new();
```
`= new()` sayesinde `Products` hiçbir zaman null değil. `category.Products.Count` güvenle çağrılabilir. Ama dikkat: **Include edilmediyse liste boş gelir**, bu da "0 ürün var" demek değildir (aşağıda Include konusuna bak).

### Product
```csharp
public int CategoryId { get; set; }
public Category Category { get; set; } = null!;
public decimal BasePrice { get; set; }      // seçeneksiz fiyat (Decorator'ın en içteki değeri)
public string? Badge { get; set; }          // Çok Satan, Yeni, Popüler, Şefin Seçimi
public bool IsVegetarian, IsCustomizable, IsActive;
```
`Product`'ta `List<OrderDetail>` yok: ürün tarafından siparişlere gitmek gerekmiyor. İlişki tek yönlü tanımlanmış (aşağıda `WithMany()` boş).

### ProductOption
```csharp
public int Id; public string GroupKey; public string Value; public decimal PriceDelta; public bool IsDefault;
```
Seçenekler **ürüne bağlı değil, global**. Bütün özelleştirilebilir ürünler aynı 9 seçeneği kullanıyor. "Bu seçenek hangi ürüne ait?" sorusu yok; ürünün `IsCustomizable` bayrağı seçeneklerin gösterilip gösterilmeyeceğini belirliyor. Basit ama sınırlı bir model: "çaya süt seçeneği gösterme" gibi bir kural için bir `ProductOptions ↔ Products` çoka-çok tablosu gerekirdi.

### Table
```csharp
public int Number; public string Area; public string QrToken; public bool IsActive = true;
public List<Order> Orders;
```
Sınıf adı `Table` ama SQL'de "TABLE" bir anahtar kelime. EF Core tablo adını `[Tables]` olarak köşeli parantezle yazdığı için sorun çıkmıyor.

### Customer
```csharp
public int Id; public string FullName; public string Phone; public List<Order> Orders;
```
Müşteri hesabı yok, şifre yok. Müşteri sipariş anında **telefon numarasına göre** bulunuyor veya oluşturuluyor.

### Order: merkez tablo
```csharp
public string OrderNo;                 // CF-1A2B3C4D (benzersiz)
public OrderChannel Channel;
public OrderStatus Status;
public int? CustomerId; public Customer? Customer;   // masa siparişinde null
public int? TableId;    public Table? Table;         // masa dışında null
public string? Address, AddressNotes;               // sadece teslimat
public DateTime? PickupTime;                        // sadece gel-al
public string? Note;
public decimal Subtotal, DeliveryFee, Total;        // hesaplanmış ve KAYDEDİLMİŞ
public DateTime CreatedAt;
public List<OrderDetail> Details;
public List<OrderStatusHistory> StatusHistories;
```
**Tasarım kararı:** Üç kanal **tek tabloda**, kanala özel kolonlar nullable. Alternatifi kalıtımdı (`DeliveryOrder : Order` gibi, TPH/TPT). Burada tür farkını Factory Method üstleniyor; veritabanı sade kalıyor.

**Neden toplamlar kaydediliyor?** `Subtotal`, `DeliveryFee` ve `Total` kalemlerden her seferinde yeniden hesaplanabilirdi. Ama teslimat ücreti kuralı yarın değişirse eski siparişin tutarı da değişirdi. Kaydedilen tutar, **o anki gerçeğin** kaydıdır (muhasebe mantığı).

### OrderDetail: sipariş kalemi
```csharp
public int OrderId; public Order Order = null!;
public int ProductId; public Product Product = null!;
public string ProductName;     // KOPYA
public decimal BasePrice;      // KOPYA (seçeneksiz fiyat)
public decimal UnitPrice;      // Decorator sonucu
public int Quantity;
public decimal LineTotal;      // UnitPrice * Quantity
public List<OrderDetailOption> Options;
```

### OrderDetailOption: kalemde seçilen seçenek
```csharp
public int OrderDetailId; public int ProductOptionId; public ProductOption ProductOption = null!;
public string GroupKey, Value; public decimal PriceDelta;   // hepsi KOPYA
```
**Snapshot (kopya) deseni:** `ProductOptionId` ile seçeneğe bağlı, ama değerleri de kopyalanmış. Yarın "Yulaf Sütü" +20 ₺ olsa bile eski fişte +15 ₺ yazar. Case'teki *"aynı ürünün farklı özelleştirmeleri ayrı sipariş kalemleri olarak saklanmalı"* maddesini bu iki tablo karşılıyor: `CF-DEMO0001`'de iki ayrı Vanilyalı Latte kalemi var, her birinin kendi seçenek satırları var.

### OrderStatusHistory
```csharp
public OrderStatus? FromStatus;   // ilk kayıtta null ("hiçbir şeyden → Received")
public OrderStatus ToStatus;
public DateTime ChangedAt;
public string? Note;
```

### Notification
```csharp
public int? OrderId; public Order? Order;
public NotificationTarget Target;   // Kitchen / Cashier / Admin
public string Message; public bool IsRead; public DateTime CreatedAt;
```

### AppUser
```csharp
public class AppUser : IdentityUser { public string FullName { get; set; } = string.Empty; }
```
`IdentityUser` hazır olarak `Id` (string GUID), `UserName`, `Email`, `PasswordHash`, `SecurityStamp`, `LockoutEnd`, `AccessFailedCount` gibi alanları getirir. Biz sadece `FullName` ekledik.

---

## 3. `CafeOrderDbContext`

```csharp
public class CafeOrderDbContext : IdentityDbContext<AppUser>
```
Normal bir DbContext `DbContext`'ten türer. `IdentityDbContext<AppUser>` ise ek olarak 7 Identity tablosunu (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`...) getirir. Kafe tabloları ile giriş tabloları **aynı veritabanında**.

```csharp
#pragma warning disable CS8618
public CafeOrderDbContext(DbContextOptions<CafeOrderDbContext> options) : base(options) { }
public DbSet<Category> Categories { get; set; }
...
#pragma warning restore CS8618
```
- `DbContextOptions` → `Program.cs`'teki `UseSqlServer(...)` ayarı buraya gelir.
- `DbSet<T>` → tablo. `_context.Orders` üzerinden LINQ sorgusu yazılır.
- `#pragma warning disable CS8618`: Nullable açık olduğu için derleyici "DbSet'ler constructor'da atanmadı" uyarısı verir. EF Core onları kendisi dolduruyor, bu yüzden uyarı kapatılmış.

```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
{
    configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
}
```
Bütün `decimal` property'leri SQL'de `decimal(18,2)` olur: toplam 18 hane, virgülden sonra 2. Bu olmasaydı EF Core her decimal için "precision belirtilmedi" uyarısı verirdi. Para için `float`/`double` **asla** kullanılmaz: `0.1 + 0.2 = 0.30000000000000004`. `decimal` ise tam sayısal kesinlik sağlar.

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);                 // 1) Identity tabloları
    modelBuilder.Entity<AppUser>().Property(u => u.FullName).HasMaxLength(100);
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(CafeOrderDbContext).Assembly);  // 2) bizimkiler
}
```
- `base.OnModelCreating` **önce** çağrılmalı. Unutulursa Identity tabloları hatalı oluşur.
- `ApplyConfigurationsFromAssembly`: assembly'deki `IEntityTypeConfiguration<T>` uygulayan bütün sınıfları bulur ve çalıştırır. Yeni bir configuration sınıfı eklediğinde buraya bir satır eklemen gerekmez.

## 4. Configuration sınıfları (Fluent API)

Kurallar entity'nin üstüne attribute (`[MaxLength(100)]`) olarak değil, ayrı sınıflarda yazılmış. Böylece EntityLayer EF Core'u hiç tanımıyor.

```csharp
// CategoryConfiguration
builder.Property(x => x.Name).HasMaxLength(100).IsRequired();   // nvarchar(100) NOT NULL
builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
builder.Property(x => x.Slug).HasMaxLength(120).IsRequired();
builder.HasIndex(x => x.Slug).IsUnique();                       // aynı slug iki kez olamaz
```
Uzunluk belirtilmeyen string'ler `nvarchar(max)` olur. `nvarchar(max)` indexlenemez ve daha yavaştır; bu yüzden aranan kolonlara uzunluk verilir.

```csharp
// OrderConfiguration
builder.Property(x => x.OrderNo).HasMaxLength(20).IsRequired();
builder.HasIndex(x => x.OrderNo).IsUnique();
builder.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20);
builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
```
`HasConversion<string>()`: enum ↔ metin dönüşümü. Bugünkü örnek veri scriptinde `'Delivered'` diye metin yazabilmemizin sebebi bu.

```csharp
// ProductConfiguration
builder.HasOne(x => x.Category).WithMany(x => x.Products)
       .HasForeignKey(x => x.CategoryId)
       .OnDelete(DeleteBehavior.Restrict);
```
**`DeleteBehavior.Restrict`**: SQL'de `ON DELETE NO ACTION`. Ürünü olan bir kategoriyi silmeye çalışırsan **veritabanı** reddeder. Varsayılan (`Cascade`) olsaydı kategoriyi silmek bütün ürünlerini de silerdi!

Burada iki katmanlı koruma var:
1. `CategoryService.DeleteAsync` önce kontrol eder ve kibar bir mesaj verir: "Sıcak Kahveler kategorisinde hâlâ ürün var".
2. O kontrol unutulsa bile veritabanı FK hatası verir. (Savunma derinliği / defense in depth.)

```csharp
// OrderDetailConfiguration
builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
// OrderDetailOptionConfiguration
builder.HasOne(x => x.ProductOption).WithMany().HasForeignKey(x => x.ProductOptionId).OnDelete(DeleteBehavior.Restrict);
```
`WithMany()` boş: `Product` tarafında `List<OrderDetail>` yok (tek yönlü ilişki). Restrict sayesinde siparişte geçen bir ürün silinemez. `ProductService.DeleteAsync` de aynı kontrolü yapıp "Make it inactive instead" diyor.

```csharp
// TableConfiguration
builder.Property(x => x.QrToken).HasMaxLength(50).IsRequired();
builder.HasIndex(x => x.QrToken).IsUnique();
```

Configuration'ı olmayan ilişkiler (Order→Customer, Order→Table, Order→Details...) convention ile kurulur. Nullable FK'ler (`CustomerId?`, `TableId?`) varsayılan olarak `ClientSetNull`, zorunlu FK'ler (`OrderDetail.OrderId`) ise `Cascade` olur: sipariş silinirse kalemleri de silinir.

## 5. Migration'lar: projenin geçmişi

| Migration | Ne yaptı |
|---|---|
| `20261109095940_InitialCreate` | 11 kafe tablosu: Categories, Customers, ProductOptions, **PromoCodes**, Tables, Products, Orders, Notifications, OrderDetails, OrderStatusHistories, OrderDetailOptions |
| `20261110125856_AddIdentity` | 7 Identity tablosu (DbContext `IdentityDbContext`'e çevrilince) |
| `20261111112347_AddProductContent` | Products'a About, Allergens, Ingredients kolonları |
| `20261112185630_RemovePromoCodes` | PromoCodes tablosu ve Orders.Discount, Orders.PromoCode kolonları **silindi** |
| `20261112191545_RemoveProductContent` | About, Allergens, Ingredients, Popularity **silindi** |

Bu tablodan öğrenilecekler:
- Proje bir ara **promosyon kodu** özelliği içeriyordu ve sonra kaldırıldı. Bunun izleri migration geçmişinde kalıyor; migration'lar **silinmez**, geri alma işlemi yeni bir migration ile yapılır.
- Her migration'ın `Up()` (uygula) ve `Down()` (geri al) metodu var. `RemovePromoCodes.Up` tabloyu siliyor, `Down` geri oluşturuyor.
- `*.Designer.cs`: o migration anındaki model'in fotoğrafı. `CafeOrderDbContextModelSnapshot.cs`: **şu anki** model. `dotnet ef migrations add X` komutu entity'leri snapshot ile karşılaştırarak farkı bulur.
- `__EFMigrationsHistory` tablosu hangi migration'ların uygulandığını tutar. Bugün şema scriptini test ederken bu tabloda 5 satır gördük.

Yeni migration eklemek için (bilgi olarak):
```bash
dotnet ef migrations add AddSomething --project CafeOrder.DataAccessLayer --startup-project CafeOrder.WebUI
```
`--startup-project WebUI` gerekli, çünkü bağlantı adresi ve DI WebUI'da.

## 6. `SeedData.EnsureSeededAsync`

```csharp
var hasData = await context.Categories.AnyAsync();
if (hasData) return;
```
Kategori tablosunda tek bir kayıt bile varsa hiçbir şey yapmaz (**idempotent**: kaç kez çalışırsa çalışsın sonuç aynı).

```csharp
var hot = new Category { Code = "CAT-001", Slug = "sicak-kahveler", Name = "Sıcak Kahveler", ... };
context.Categories.Add(hot);
...
context.Products.Add(CreateProduct(hot, "Klasik Cappuccino", 145, "1572442388796-...", "...", null, true, true));
```
- `CreateProduct(hot, ...)` içinde `product.Category = hot` atanıyor; **CategoryId değil**, nesnenin kendisi. Henüz `hot.Id` yok (0), çünkü veritabanına yazılmadı. EF Core sıralamayı kendisi çözer: önce kategoriyi INSERT eder, oluşan Id'yi alır ve ürünün `CategoryId`'sine yazar. Buna **relationship fix-up** denir.
- 8 kategori (Seasonal pasif), 20 ürün (7 tanesi `IsCustomizable`), 9 seçenek, 2 masa.
- En sonda **tek** `SaveChangesAsync()`. Bütün INSERT'ler tek seferde gönderilir ve EF Core bunları otomatik bir transaction'a sarar.
- `ImageUrl(id)`: Unsplash adresi üretir. Resimler internetten gelir; internet yoksa `site.js`'teki `data-fallback` mekanizması ikon gösterir.

Seed edilen Id'ler (bugün sorguladık): ürünler 1–20, seçenekler 1=Küçük, 2=Orta(+15, varsayılan), 3=Büyük(+30), 4=Normal Süt(varsayılan), 5=Yulaf Sütü(+15), 6=Badem Sütü(+15), 7=Ekstra shot "Evet"(+20), 8=Vanilya Şurubu(+10), 9=Karamel Şurubu(+10).

## 7. Repository'ler

### `IRepository<TEntity>`: generic arayüz
```csharp
public interface IRepository<TEntity> where TEntity : class
{
    Task<List<TEntity>> GetAllAsync();
    Task<TEntity?> GetByIdAsync(int id);
    Task CreateAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(TEntity entity);
    Task<List<TEntity>> GetWhereAsync(Expression<Func<TEntity, bool>> predicate);
    Task<TEntity?> GetFirstAsync(Expression<Func<TEntity, bool>> predicate);
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null);
    IQueryable<TEntity> GetQueryable();
}
```
- `where TEntity : class`: generic kısıt; sadece referans tipleri (`Order`, `Product`...) kullanılabilir.
- **`Expression<Func<T,bool>>` ile `Func<T,bool>` arasındaki fark (çok önemli):**
  - `Func<Order,bool>` derlenmiş bir koddur. EF Core içine bakamaz; bütün tabloyu çekip RAM'de filtrelemek zorunda kalır.
  - `Expression<Func<Order,bool>>` bir **kod ağacıdır** (veri olarak kod). EF Core bu ağacı okuyup SQL `WHERE` cümlesine çevirir.
  - `GetFirstAsync(t => t.QrToken == token && t.IsActive)` → `SELECT TOP(1) ... WHERE QrToken = @token AND IsActive = 1`.
- `GetQueryable()`: sorguyu **çalıştırmadan** döndürür. Servis üzerine `.Include(...).Where(...).OrderBy(...)` ekler, `ToListAsync()` denince tek bir SQL üretilir (**deferred execution**, ertelenmiş çalışma).

### `GenericRepository<TEntity>`
```csharp
protected readonly CafeOrderDbContext _context;
public async Task<List<TEntity>> GetAllAsync() => await _context.Set<TEntity>().ToListAsync();
public async Task<TEntity?> GetByIdAsync(int id) => await _context.Set<TEntity>().FindAsync(id);
public async Task CreateAsync(TEntity entity) { await _context.AddAsync(entity); await _context.SaveChangesAsync(); }
public async Task UpdateAsync(TEntity entity) { _context.Update(entity); await _context.SaveChangesAsync(); }
public async Task DeleteAsync(TEntity entity) { _context.Remove(entity); await _context.SaveChangesAsync(); }
```
- `_context.Set<TEntity>()`: generic olarak `DbSet`'e ulaşmanın yolu (`_context.Orders` yazamayız, çünkü T'nin ne olduğunu bilmiyoruz).
- `FindAsync(id)`: önce DbContext'in **hafızasına** (change tracker) bakar; bu istekte o kayıt zaten yüklendiyse SQL'e gitmez.
- **Her metot kendi `SaveChangesAsync`'ini çağırıyor.** Basit ama bir sonucu var: bir iş birden fazla repository çağrısından oluşuyorsa her biri ayrı ayrı kaydedilir. Örnek: sipariş kaydedildikten sonra 3 observer 3 ayrı `CreateAsync` → 3 ayrı `SaveChanges` → 3 ayrı veritabanı turu. Alternatifi **Unit of Work** desenidir: repository'ler sadece ekler, en sonda tek bir `SaveChanges` çağrılır.
- `protected` `_context`: alt sınıflar (`OrderRepository`) kullanabilsin diye.

### Change Tracker'ı anlamak (EF Core'un kalbi)
DbContext yüklediği her entity'yi izler ve durumunu tutar: `Unchanged`, `Modified`, `Added`, `Deleted`. `SaveChanges` bu durumlara bakarak INSERT/UPDATE/DELETE üretir.
- `GetByIdAsync` ile yüklenen kategori `Unchanged` durumundadır. `category.IsActive = false` yazdığında EF Core bunu fark eder ve `Modified` yapar.
- `_context.Update(entity)` ise grafikteki **her şeyi** `Modified` işaretler; Id'si 0 olan yeni nesneleri ise `Added` yapar. Bölüm 05'te `ChangeStatusAsync`'te yeni `OrderStatusHistory` (Id=0) tam olarak bu sayede INSERT ediliyor.

### Tabloya özel repository'ler
```csharp
public interface ICategoryRepository : IRepository<Category> { }
public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
{
    public CategoryRepository(CafeOrderDbContext context) : base(context) { }
}
```
Boş görünüyorlar, neden varlar? İki sebebi var:
1. DI'a `ICategoryRepository` olarak kaydediliyorlar. Servis "kategori repository'si istiyorum" diyor, "generic repository of Category" değil. Okunması daha net.
2. Tabloya özel bir metot gerekince yeri hazır: `IOrderRepository` buna örnek.

### `OrderRepository.CreateWithTransactionAsync`
```csharp
public async Task CreateWithTransactionAsync(Order order)
{
    await using var transaction = await _context.Database.BeginTransactionAsync();
    await _context.Orders.AddAsync(order);
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
```
- `AddAsync(order)`: sadece siparişi değil, **bütün grafiği** `Added` yapar: `order.Details`, her kalemin `Options`'ı, `order.StatusHistories` ve yeni bir müşteriyse `order.Customer`.
- `SaveChangesAsync`: EF Core sırayı çözer. Önce Customer INSERT edilir (Id alınır), sonra Order (CustomerId ile), sonra OrderDetails (OrderId ile), sonra OrderDetailOptions, sonra OrderStatusHistories.
- `await using`: blok bitince transaction dispose edilir. `CommitAsync` çağrılmadan bir exception olursa dispose sırasında **rollback** yapılır.
- **Dürüst not:** EF Core tek bir `SaveChanges`'i zaten otomatik olarak bir transaction içinde çalıştırır. Yani buradaki açık transaction teknik olarak aynı garantiyi verir. Açık yazılmış olması niyeti belli ediyor (case "transaction yaklaşımı" istiyor) ve ileride araya ikinci bir `SaveChanges` eklenirse (örneğin bildirimleri de aynı transaction'a almak) hazır olması açısından değerli.

---

## 8. Kendini sına
1. `Order.Total` neden hesaplanmak yerine kaydediliyor?
2. `DeleteBehavior.Restrict` olmasaydı kategori silindiğinde ne olurdu?
3. `GetFirstAsync(x => ...)` neden `Expression<Func<...>>` alıyor? `Func` alsaydı ne değişirdi?
4. Seed'de `product.Category = hot` atanırken `hot.Id` sıfır. Bu neden sorun değil?
5. `CreateWithTransactionAsync` içindeki `SaveChangesAsync` kaç INSERT üretir? (İpucu: 3 kalemli, ilk kalemi 3 seçenekli, yeni müşterili bir teslimat siparişini düşün.)
