# 01 · Solution, katmanlar ve Program.cs

> Bu bölümde projenin iskeletini öğreniyoruz: hangi proje neyi biliyor, uygulama açılırken ne oluyor, bir istek sunucuya geldiğinde hangi sırayla nereden geçiyor.

---

## 1. Solution dosyası: `CafeOrder.slnx`

```xml
<Solution>
  <Project Path="CafeOrder.BusinessLayer/CafeOrder.BusinessLayer.csproj" />
  <Project Path="CafeOrder.DataAccessLayer/CafeOrder.DataAccessLayer.csproj" />
  <Project Path="CafeOrder.DtoLayer/CafeOrder.DtoLayer.csproj" />
  <Project Path="CafeOrder.EntityLayer/CafeOrder.EntityLayer.csproj" />
  <Project Path="CafeOrder.WebUI/CafeOrder.WebUI.csproj" />
</Solution>
```

`.slnx`, Visual Studio'nun yeni XML tabanlı solution biçimidir; eski `.sln` dosyasıyla aynı işi görür. 5 projeyi bir arada açar. Projeler birbirine **`ProjectReference`** ile bağlanır ve bu bağlantılar mimarinin kendisidir.

## 2. Kim kimi tanıyor? (csproj referansları)

```text
EntityLayer        → (hiçbir projeyi tanımaz)  + Microsoft.Extensions.Identity.Stores
DtoLayer           → EntityLayer                (sadece enum'lar için)
DataAccessLayer    → EntityLayer                + EF Core SqlServer + Identity.EntityFrameworkCore
BusinessLayer      → DataAccessLayer, DtoLayer, EntityLayer + Mapster
WebUI              → hepsi                      + EF Core Design (migration komutları için)
```

Bu tablonun neden önemli olduğuna dair üç nokta:

- **EntityLayer en altta, hiçbir şey bilmiyor.** `Order`, `Product` gibi sınıflar saf C# sınıfları (POCO). Tek istisna `AppUser : IdentityUser`; bu yüzden EntityLayer'a `Microsoft.Extensions.Identity.Stores` paketi eklenmiş (`IdentityUser` sınıfı o pakette).
- **DtoLayer neden EntityLayer'ı tanıyor?** DTO'larda `OrderChannel`, `OrderStatus` gibi enum'lar kullanılıyor ve enum'lar EntityLayer'da. DTO'lar entity *sınıflarını* kullanmıyor, sadece enum'ları.
- **DataAccessLayer, BusinessLayer'ı tanımıyor.** Repository iş kuralı bilmez, sadece veri okur ve yazar.
- **WebUI neden DataAccessLayer'ı tanıyor?** `Program.cs`'te `CafeOrderDbContext` ve repository'ler DI'a kaydediliyor. Kayıt yapabilmek için sınıfları görmesi gerekiyor. Controller'lar ise repository'yi hiç kullanmıyor, sadece servisleri kullanıyor.

Bütün projelerde şu ayarlar var:
```xml
<TargetFramework>net8.0</TargetFramework>
<ImplicitUsings>enable</ImplicitUsings>   <!-- System, System.Linq, System.Threading.Tasks... otomatik using -->
<Nullable>enable</Nullable>               <!-- string? ile string ayrımı; null uyarıları -->
```

`Nullable` açık olduğu için kodda sürekli `string?`, `= string.Empty` ve `= null!` göreceksin:
- `public string Name { get; set; } = string.Empty;` → "bu asla null olmaz, başlangıçta boş metin."
- `public string? Description { get; set; }` → "null olabilir" (veritabanında NULL kabul eden kolon).
- `public Category Category { get; set; } = null!;` → "derleyici, bana güven: bunu EF Core dolduracak." `null!` "null-forgiving" operatörüdür.

WebUI'daki `<UserSecretsId>216c75ab-...</UserSecretsId>` satırı User Secrets'ın hangi klasörde tutulacağını belirler (`%APPDATA%\Microsoft\UserSecrets\216c75ab...\secrets.json`). Admin şifresi ve bu bilgisayarın bağlantı adresi orada duruyor; repoya girmiyor.

---

## 3. Ayar dosyaları

### `appsettings.json`
```json
"ConnectionStrings": { "DefaultConnection": "Server=KORAYHAN\\SQLEXPRESS;Database=CafeOrderDb;Trusted_Connection=True;TrustServerCertificate=True" },
"Pricing": { "DeliveryFee": 30, "FreeDeliveryFrom": 600, "MinDeliveryAmount": 100 },
```
- `Trusted_Connection=True`: Windows oturumunla bağlan (SQL kullanıcı adı ve şifresi yok).
- `TrustServerCertificate=True`: SQL Express'in kendi imzalı sertifikasını kabul et. EF Core 8 / SqlClient 5 varsayılan olarak şifreli bağlantı istiyor; bu satır olmazsa "certificate chain" hatası alırsın.
- `Pricing`: Strategy ve Chain'in kullandığı rakamlar (Bölüm 05).

### `appsettings.Development.json`
```json
"AdminAccount": { "Email": "admin@cafeflow.test", "FullName": "Selin Aydın" }
```
Bu dosya sadece `ASPNETCORE_ENVIRONMENT=Development` iken okunur (`launchSettings.json` bunu ayarlıyor).

### Ayarlar hangi sırayla birleşir? (önemli)
ASP.NET Core ayarları katman katman okur ve **sonraki öncekinin üzerine yazar**:
```text
appsettings.json → appsettings.{Environment}.json → User Secrets (sadece Development) → Ortam değişkenleri → komut satırı
```
Bugün bunu iki kez kullandık:
1. İlk testte `ConnectionStrings__DefaultConnection` ortam değişkeni verdik (`__` iki nokta yerine geçer). `KORAYHAN` adresi ezildi.
2. Sonra aynı değeri User Secrets'a yazdık. Artık bu bilgisayarda değişkene gerek kalmadan `DESKTOP-NDMQ7CM` kullanılıyor; diğer bilgisayarında `appsettings.json`'daki değer geçerli.

Admin şifresi de aynı yoldan geliyor: `AdminAccount:Password` User Secrets'ta, `Email` ve `FullName` ise `appsettings.Development.json`'da. `GetSection("AdminAccount")` ikisini birleştirilmiş olarak görür.

---

## 4. `Program.cs`, satır satır

### 4.1 Dil ve sayı biçimi: global kültür ayarı YOK
Site tamamen Türkçe. Eskiden `Program.cs`'in başında bütün uygulamayı İngilizce (`en-US`) kültüre zorlayan iki satır vardı; o satırlar **kaldırıldı**. Global kültür ayarı yerine şu üç parça kullanılıyor, böylece site sunucunun dili ne olursa olsun aynı görünüyor:

1. **Ekrana yazarken:** Para ve tarih her zaman yardımcı metotlardan geçiyor ve bu metotlar Türkçe kültürü **açıkça** kullanıyor:
   ```csharp
   private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
   public static string Money(decimal amount) => "₺" + amount.ToString("N2", Turkish);   // ₺1.234,50
   public static string Date(DateTime value) => value.ToString("d MMM yyyy, HH:mm", Turkish);   // 13 Kas 2026, 10:30
   ```
   (`WebUI/Models/Display.cs` müşteri tarafı, `WebUI/Models/AdminDisplay.cs` admin tarafı.)
2. **Formdan okurken:** `WebUI/ModelBinding/DecimalModelBinder.cs`. Varsayılan model binder sayıyı sunucunun diline göre okur. Türkçe bir sunucuda `.` binlik ayırıcı olduğu için tarayıcının sayı kutusundan gelen `185.50` değeri **18550** okunurdu. Özel binder hem `185,50` hem `185.50` yazımını doğru okuyor (son ayırıcı ondalık sayılıyor):
   ```csharp
   builder.Services.AddControllersWithViews(options =>
       options.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider()));
   ```
   `Insert(0, ...)`: listenin başına eklenir, böylece decimal alanlar için varsayılan binder'dan önce bu çalışır. Bu da bir **Chain of Responsibility**: MVC sağlayıcıları sırayla gezer ve "ben bağlarım" diyen ilk sağlayıcı işi alır.
3. **Forma geri yazarken:** `<input type="number">` her dilde noktalı değer bekler. Ürün formunda fiyat `Model.BasePrice.ToString(CultureInfo.InvariantCulture)` ile yazılıyor; virgüllü yazılsaydı kutu boş görünürdü.

Tarih alanları (`datetime-local` → `PickupTime`) tarayıcıdan ISO biçiminde (`2026-11-13T10:30`) gelir ve her kültürde aynı okunur, bu yüzden ek bir şey gerekmiyor.

Enum'ların ekrandaki adları için `EntityLayer/Enums/EnumTexts.cs` var: `OrderStatus.Preparing.ToText()` → "Hazırlanıyor". Veritabanında ve kodda enum adı (`Preparing`) kalıyor; kullanıcı her zaman Türkçe metni görüyor. Bu sınıf en alttaki katmanda, çünkü hem BusinessLayer (hata ve bildirim mesajları) hem WebUI (sayfalar) kullanıyor.

### 4.2 Builder ve MVC
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews(options =>
    options.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider()));
```
`CreateBuilder` ayarları, loglamayı ve DI kabını hazırlar. `AddControllersWithViews` MVC'yi (controller + Razor view) ekler. Razor Pages veya API-only değil, klasik MVC. İçine verilen ayar, decimal binder'ı ekliyor (4.1).

### 4.3 DbContext
```csharp
builder.Services.AddDbContext<CafeOrderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```
`AddDbContext` DbContext'i **Scoped** olarak kaydeder: her HTTP isteği için bir tane oluşur ve istek bitince atılır. Aynı istekte `OrderRepository`, `ProductRepository` ve `NotificationRepository` **aynı DbContext nesnesini** paylaşır. Bu ayrıntı çok önemli, Bölüm 05'te bu sayede çalışan bir şey göreceğiz.

### 4.4 Session
```csharp
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => options.IdleTimeout = TimeSpan.FromHours(2));
```
Session verisinin bir yerde saklanması gerekir; `AddDistributedMemoryCache` sunucunun RAM'ini kullanır. Bunun sonuçları:
- Uygulama yeniden başlarsa **bütün sepetler silinir**.
- Birden fazla sunucuya yayılsaydık (load balancer) sepetler kaybolurdu; o durumda Redis veya SQL kullanılır.
- Tarayıcıda sadece `.AspNetCore.Session` adlı bir çerez durur, içinde sadece session kimliği vardır. Sepetin kendisi sunucudadır.
- 2 saat boyunca istek gelmezse session silinir.

### 4.5 Identity
```csharp
builder.Services
    .AddIdentity<AppUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddEntityFrameworkStores<CafeOrderDbContext>()
    .AddErrorDescriber<TurkishIdentityErrorDescriber>()
    .AddClaimsPrincipalFactory<AppClaimsPrincipalFactory>();
```
- `AddErrorDescriber<TurkishIdentityErrorDescriber>()`: Identity'nin hata mesajları ("Passwords must have at least one digit" gibi) varsayılan olarak İngilizcedir. `WebUI/Identity/TurkishIdentityErrorDescriber.cs`, `IdentityErrorDescriber`'ın metotlarını ezerek bunları Türkçe veriyor ("Şifre en az bir rakam içermeli.").
- `AddIdentity<AppUser, IdentityRole>`: kullanıcı tipimiz `AppUser`, rol tipimiz hazır `IdentityRole`. `UserManager<AppUser>`, `SignInManager<AppUser>` ve `RoleManager<IdentityRole>` DI'a eklenir.
- Şifre kuralları: Identity varsayılan olarak büyük harf, küçük harf, rakam ve sembol ister. Burada sadece uzunluk 8'e çıkarılmış; diğer kurallar varsayılan olarak açık kalıyor. Bu yüzden şifre en az 8 karakter olmalı ve büyük harf, küçük harf, rakam ve sembol içermeli (örnek biçim: `Ornek123!`).
- Lockout: 5 yanlış denemede 5 dakika kilit. Bugün iki kez yanlış şifre denedik; `AccessFailedCount = 2` olmuştu, ki 5'e ulaşmadan durmamız gerekiyordu.
- `AddEntityFrameworkStores<CafeOrderDbContext>`: kullanıcılar `AspNetUsers` tablosunda.
- `AddClaimsPrincipalFactory<AppClaimsPrincipalFactory>`: giriş çerezine `full_name` claim'i ekleyen özel sınıf (Bölüm 07).

### 4.6 Çerez ayarları
```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.LoginPath = "/admin/login";
    options.LogoutPath = "/admin/logout";
    options.AccessDeniedPath = "/admin/access-denied";
});
```
- `SlidingExpiration`: sürenin yarısı geçtikten sonra gelen bir istek, çerezi 30 güne yeniden uzatır.
- `LoginPath`: `[Authorize]` olan bir sayfaya girişsiz gelinirse buraya `?ReturnUrl=` ile yönlendirilir. Testte `/admin/orders` → `302 /admin/login?ReturnUrl=%2Fadmin%2Forders` gördük; o davranış buradan geliyor.
- `AccessDeniedPath`: giriş yapmış ama Admin rolü olmayan kullanıcının gönderildiği yer.

### 4.7 PricingOptions: Options deseninin basit hali
```csharp
builder.Services.AddSingleton(builder.Configuration.GetSection("Pricing").Get<PricingOptions>() ?? new PricingOptions());
```
`GetSection("Pricing").Get<PricingOptions>()` JSON'u bir C# nesnesine çevirir (configuration binding). `?? new PricingOptions()` ise bölüm yoksa sınıftaki varsayılanları (30/600/100) kullanır. **Singleton** olarak kaydedilir: uygulama boyunca tek nesne vardır.

Not: Microsoft'un önerdiği yol `builder.Services.Configure<PricingOptions>(...)` ve `IOptions<PricingOptions>` kullanmaktır. Burada sade olsun diye nesnenin kendisi kaydediliyor. Bunun sonucu: `appsettings.json` uygulama çalışırken değiştirilirse yeni değerler okunmaz, uygulamayı yeniden başlatmak gerekir.

### 4.8 Mapster
```csharp
TypeAdapterConfig mapsterConfig = TypeAdapterConfig.GlobalSettings;
mapsterConfig.Scan(typeof(GeneralMapping).Assembly);
builder.Services.AddSingleton(mapsterConfig);
builder.Services.AddScoped<IMapper, ServiceMapper>();
```
"`GeneralMapping`'in bulunduğu assembly'deki bütün `IRegister` sınıflarını tara, kurallarını ayarlara ekle ve `IMapper`'ı DI'a ekle." Bölüm 03'te ayrıntısı var (AutoMapper'dan neden geçildiği dahil).

### 4.9 Repository ve servis kayıtları
```csharp
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
...
builder.Services.AddScoped<IOrderService, OrderService>();
```
Hepsi **Scoped**. Neden? Repository'ler `DbContext`'e bağımlı ve DbContext scoped. Bir singleton servis scoped bir DbContext tutarsa DbContext istekler arasında paylaşılır ve thread-safe olmadığı için çöker ("captive dependency" hatası).

Üç ömür tipi:
| Ömür | Ne zaman oluşur | Örnek |
|---|---|---|
| Transient | Her istendiğinde yeni | (projede yok) |
| Scoped | Her HTTP isteğinde bir tane | DbContext, repository'ler, servisler, observer'lar |
| Singleton | Uygulama boyunca tek | `PricingOptions`, `IMapper` |

### 4.10 Observer kayıtları: aynı interface'e üç sınıf
```csharp
builder.Services.AddScoped<IOrderObserver, KitchenObserver>();
builder.Services.AddScoped<IOrderObserver, CashierObserver>();
builder.Services.AddScoped<IOrderObserver, AdminObserver>();
builder.Services.AddScoped<OrderEventPublisher>();
```
Bir interface'e birden fazla kayıt yapınca:
- `IOrderObserver` istersen **son kayıt** gelir (`AdminObserver`).
- `IEnumerable<IOrderObserver>` istersen **hepsi**, kayıt sırasıyla gelir.

`OrderEventPublisher` constructor'da `IEnumerable<IOrderObserver>` istiyor. Observer deseninin "abone listesi" DI kabının ta kendisi oluyor. `OrderEventPublisher` interface'siz, doğrudan sınıf olarak kaydedilmiş; bu da geçerli bir kullanım.

### 4.11 Build ve açılış işleri
```csharp
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CafeOrderDbContext>();
    await context.Database.MigrateAsync();
    await SeedData.EnsureSeededAsync(context);
    await AdminAccountSeeder.EnsureSeededAsync(scope.ServiceProvider, app.Logger);
}
```
- `Build()` sonrasında servis kaydı yapılamaz. Artık kap kilitli.
- **Neden `CreateScope`?** DbContext scoped. Ortada HTTP isteği olmadığı için bir scope'u elle açıyoruz; `using` bitince DbContext atılıyor.
- `MigrateAsync()`: veritabanı yoksa oluşturur, `__EFMigrationsHistory` tablosuna bakar ve uygulanmamış migration'ları sırayla uygular. Bugün `CafeOrderDb` olmadığı için 5 migration'ın hepsi bu satırda uygulandı.
- `SeedData.EnsureSeededAsync`: Categories boşsa örnek menüyü ekler (Bölüm 02).
- `AdminAccountSeeder`: Admin rolü ve ilk admin (Bölüm 07). Bugün admin kaydını silip uygulamayı yeniden başlattığımızda bu satır hesabı yeni şifreyle tekrar oluşturdu.

> Üretim notu: Uygulama açılışında otomatik migration ders ve geliştirme ortamı için çok pratik. Gerçek üretimde genelde migration'lar ayrı bir adımda (`dotnet ef database update` veya SQL scripti) çalıştırılır. Bugün ürettiğimiz `database/CafeOrderDb_schema.sql` bunun için.

### 4.12 Middleware hattı (sıra önemli!)
```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/status/{0}");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```
Her istek bu boru hattından yukarıdan aşağı geçer; cevap ise aşağıdan yukarı döner. (Bu da bir Chain of Responsibility örneği!)

| Middleware | Görevi |
|---|---|
| `UseExceptionHandler("/error")` | Sadece Development dışında. Yakalanmamış bir exception olursa `/error` sayfasını gösterir. Development'ta ise ayrıntılı hata sayfası çıkar. |
| `UseHsts` | Tarayıcıya "bu siteye hep HTTPS ile gel" der. |
| `UseStatusCodePagesWithReExecute("/status/{0}")` | Cevap gövdesiz bir 404/400 ise isteği `/status/404` adresine **yeniden çalıştırır**. Adres çubuğu değişmez. Testte boş sepet POST'unda gördüğümüz "Oops, a hiccup (error 400)" sayfası buradan geliyordu (antiforgery token eksikti). |
| `UseHttpsRedirection` | HTTP'yi HTTPS'e yönlendirir. `http` profilinde HTTPS portu tanımlı olmadığı için uyarı verip geçer. |
| `UseStaticFiles` | `wwwroot` altındaki css/js/ico dosyalarını doğrudan verir. Controller'a hiç uğramaz. |
| `UseRouting` | URL'ye bakıp hangi controller action'ının çalışacağını **belirler** (henüz çalıştırmaz). |
| `UseSession` | Session çerezini okur ve `HttpContext.Session`'ı hazırlar. Controller'dan önce olmalı. |
| `UseAuthentication` | Giriş çerezini çözer ve `HttpContext.User`'ı doldurur. |
| `UseAuthorization` | Seçilen action'da `[Authorize(Roles="Admin")]` varsa kontrol eder: giriş yoksa 302 → login, rol yoksa → access-denied. Routing'den **sonra** olmalı, çünkü hangi action'ın seçildiğini bilmesi gerekiyor. |
| `MapControllers` | Seçilen action'ı çalıştırır. |

### 4.13 `MapControllers`: projede klasik route yok!
Çoğu MVC projesinde şunu görürsün:
```csharp
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
```
Bu projede **yok**. Bütün adresler **attribute routing** ile, controller'ların üzerinde yazılı:
```csharp
[Route("cart")] public class CartController       → [HttpGet("")] /cart, [HttpPost("add")] /cart/add
[Route("products")] ...                           → [HttpGet("{id:int}")] /products/7
[HttpGet("/")] HomeController.Index               → /
[Route("admin/orders")] (Area=Admin)              → /admin/orders, /admin/orders/5/status
```
Avantajı: her action'ın adresi, kodun hemen üstünde görünür. `{id:int}` bir **route constraint**'tir: `/products/abc` bu action'a hiç eşleşmez ve 404 olur.

---

## 5. Bir isteğin tam yolculuğu (özet)

`GET /products/2` için:
```text
Kestrel (web sunucusu) isteği alır
 → Middleware: StaticFiles (dosya değil, geç) → Routing (ProductsController.Detail seçildi, id=2)
 → Session → Authentication (giriş yok, sorun değil) → Authorization ([Authorize] yok, geç)
 → DI: yeni scope açılır. ProductsController oluşturulurken constructor'ı IProductService ve ICategoryService ister
       → ProductService oluşturulur; o da IProductRepository, ICategoryRepository... ister
       → repository'ler oluşturulur; onlar CafeOrderDbContext ister → bu istek için TEK bir DbContext
 → Detail(2) çalışır → servis → repository → EF Core → SQL → entity → Mapster → DTO
 → return View(product) → Razor, Views/Products/Detail.cshtml'i HTML'e çevirir
 → cevap middleware'lerden geri çıkar → tarayıcı
 → scope kapanır: DbContext ve bütün scoped nesneler Dispose edilir
```
Bu zincirdeki **constructor injection** her katmanda aynı. Hiçbir sınıf `new ProductRepository()` demiyor; ihtiyacını constructor'da istiyor, DI kabı veriyor. **Dependency Inversion** (SOLID'deki D) budur: üst katman somut sınıfa değil, interface'e bağımlı.

İstisna: desen sınıfları (`new DeliveryFeeStrategy(...)`, `new ProductHandler()`, `new OptionPriceDecorator(...)`) `OrderService` içinde `new` ile oluşturuluyor. Bu bilinçli bir sadelik tercihi; Bölüm 05'te tartışıyoruz.
