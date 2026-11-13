# 07 · Admin paneli ve Identity

> Bu bölüm `/admin` altındaki her şeyi kapsıyor: giriş sistemi (ASP.NET Core Identity), ilk admin hesabının oluşturulması, Area yapısı, admin controller'ları, layout ve view component'ler.

---

## 1. Area nedir?

```text
CafeOrder.WebUI/
  Controllers/         ← müşteri
  Views/               ← müşteri
  Areas/
    Admin/
      Controllers/     ← admin
      Models/
      ViewComponents/
      Views/           ← admin (kendi _ViewStart ve _ViewImports'u var)
```
Area, büyük bir uygulamayı bölümlere ayırır. Admin'in kendi layout'u (`_AdminLayout`), kendi `_ViewImports`'u ve kendi modelleri var. İki tarafta da `ProductsController` var (müşteri menüsü ve admin ürün yönetimi); farklı namespace'lerde oldukları ve farklı route'ları olduğu için çakışmıyorlar.

`[Area("Admin")]` attribute'u MVC'ye "bu controller'ın view'larını `Areas/Admin/Views/...` altında ara" der. Bu attribute `AdminControllerBase`'te tanımlı, ve bütün admin controller'ları ondan türüyor.

---

## 2. `AdminControllerBase`: güvenliği unutulmaz kılmak

```csharp
[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public abstract class AdminControllerBase : Controller
{
}
```
- **`[Authorize]` attribute'u kalıtımla geçer.** `CategoriesController : AdminControllerBase` dediğin anda kilitli olur. Yeni bir admin controller'ı yazıp `[Authorize]` yazmayı unutmak mümkün değil. Bu "secure by default" (varsayılan olarak güvenli) yaklaşımıdır.
- `Roles = AppRoles.Admin`: giriş yapmış olmak yetmez, **Admin rolü** de gerekir.
- `abstract`: tek başına bir controller olarak kullanılamaz.
- Sınıfın içi boş: görevi sadece iki attribute'u bütün admin controller'larına miras bırakmak.

### Admin panelinde mesajlar nasıl gösteriliyor?
Admin tarafında **TempData kullanılmıyor**. Kural basit:
- **Başarılı işlem** (kaydet, sil, aktif/pasif, durum değiştir): sadece listeye veya detay sayfasına **yönlendirilir** (PRG). Ayrıca mesaj gösterilmez; değişiklik sayfada zaten görünür.
- **Kural hatası** (`BusinessException`, ör. "kategoride hâlâ ürün var"): yönlendirme **yapılmaz**. Controller mesajı `ViewBag.Error`'a koyar ve sayfayı aynı istek içinde doğrudan çizer. Liste sayfaları için her controller'da küçük bir yardımcı var:
```csharp
private async Task<IActionResult> ShowListWithErrorAsync(string error)
{
    ViewBag.Error = error;
    return View("Index", await _categoryService.GetAllAsync());
}
```
- Mesajı `_ErrorMessage.cshtml` partial'ı gösteriyor. Model olarak sadece mesaj metnini alıyor; boşsa hiçbir şey çizmiyor:
```razor
@model string?
@if (!string.IsNullOrEmpty(Model))
{
  <div class="adm-callout warn mb-4" role="alert">...@Model...</div>
}
```
View'larda: `<partial name="_ErrorMessage" model="@((string?)ViewBag.Error)" />`.
- Şifre değiştirme başarılı olunca Hesabım sayfası `ResultAccountDto.Succeeded = true` ile doğrudan çizilir ve sayfa "Şifreniz değiştirildi." yazar.

(Müşteri tarafındaki sepet bildirimi, "Ürün sepete eklendi" yazısı, hâlâ TempData ile çalışıyor; Bölüm 06.)

---

## 3. Identity: giriş sistemi

### Tablolar (`AddIdentity` migration'ı)
| Tablo | İçerik |
|---|---|
| `AspNetUsers` | Kullanıcılar: UserName, Email, **PasswordHash**, SecurityStamp, LockoutEnd, AccessFailedCount, FullName (bizim eklediğimiz) |
| `AspNetRoles` | Roller: "Admin" |
| `AspNetUserRoles` | Kullanıcı ↔ rol (çoka-çok) |
| `AspNetUserClaims`, `AspNetRoleClaims`, `AspNetUserLogins`, `AspNetUserTokens` | Projede kullanılmıyor (dış giriş, 2FA vb. için) |

### Şifre nasıl saklanıyor?
Şifre **asla** düz metin olarak saklanmıyor. Identity'nin `PasswordHasher`'ı PBKDF2 (HMAC-SHA512, rastgele salt, çok sayıda iterasyon) kullanıyor. `PasswordHash` kolonunda `AQAAAAIAAYagAAAAE...` gibi bir değer durur. Giriş sırasında yazılan şifre aynı salt ile tekrar hash'lenir ve karşılaştırılır. Veritabanı çalınsa bile şifreler doğrudan okunamaz.

Bugün yaşadığımız durumu hatırla: User Secrets'taki şifre, kullanmak istediğimiz şifre değildi. Veritabanındaki hash'ten şifreyi "geri okumak" mümkün olmadığı için yapılabilecek tek şey hesabı yeni şifreyle yeniden oluşturmaktı.

### `AdminAccountOptions` ve `AdminAccountSeeder`
```csharp
public class AdminAccountOptions
{
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string FullName { get; set; } = "Cafe Manager";
}
```
```csharp
public static async Task EnsureSeededAsync(IServiceProvider services, ILogger logger)
{
    var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
    var users = services.GetRequiredService<UserManager<AppUser>>();
    var options = services.GetRequiredService<IConfiguration>().GetSection("AdminAccount").Get<AdminAccountOptions>();

    if (!await roles.RoleExistsAsync(AppRoles.Admin))
        await roles.CreateAsync(new IdentityRole(AppRoles.Admin));               // 1) rol

    if ((await users.GetUsersInRoleAsync(AppRoles.Admin)).Count > 0)
        return;                                                                   // 2) zaten admin varsa dur

    if (string.IsNullOrWhiteSpace(options?.Email) || string.IsNullOrWhiteSpace(options.Password))
    {
        logger.LogWarning("No admin account exists and the AdminAccount settings are empty, ...");
        return;                                                                   // 3) ayar yoksa uyar ve geç
    }

    var admin = new AppUser { UserName = options.Email, Email = options.Email, EmailConfirmed = true, FullName = options.FullName };
    var created = await users.CreateAsync(admin, options.Password);              // 4) şifre burada hash'lenir
    if (!created.Succeeded) { logger.LogError(...); return; }
    await users.AddToRoleAsync(admin, AppRoles.Admin);                           // 5) role ekle
}
```
- **Servis locator kullanımı:** `GetRequiredService` ile servisler elle isteniyor. Bu normalde kaçınılan bir yöntemdir, ama burada bir controller veya constructor yok (açılışta çalışan statik bir metot), bu yüzden kabul edilebilir.
- **"Hiç admin yoksa"** kuralı: User Secrets'taki şifreyi değiştirmek mevcut admin'in şifresini **değiştirmez**. Bugün bu yüzden önce kullanıcı kaydını silmemiz gerekti. Şifre panelden "Hesabım" sayfasıyla değiştirilir.
- `UserName = Email`: kullanıcı e-postayla giriş yapıyor.
- `users.CreateAsync(admin, password)`: Identity şifre kurallarını burada kontrol eder (8 karakter, büyük harf, küçük harf, rakam, sembol). Şifre zayıfsa `created.Succeeded = false` olur ve hata loglanır.

### `AppClaimsPrincipalFactory`: çereze ad yazmak
```csharp
public class AppClaimsPrincipalFactory : UserClaimsPrincipalFactory<AppUser, IdentityRole>
{
    public const string FullNameClaim = "full_name";
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);   // Id, UserName, roller, SecurityStamp...
        identity.AddClaim(new Claim(FullNameClaim, string.IsNullOrWhiteSpace(user.FullName) ? user.UserName ?? "Admin" : user.FullName));
        return identity;
    }
}
```
**Claim**, kullanıcı hakkında "anahtar = değer" biçiminde bir bilgidir. Giriş sırasında claim'ler şifrelenip çereze yazılır. Sonraki her istekte `UseAuthentication` çerezi çözer ve `User.FindFirst("full_name")` ile ad okunabilir. Sidebar ve üst çubuktaki "Selin Aydın" yazısı için her sayfada veritabanına gidilmiyor.

Bedeli: Ad değişirse çerez yenilenene kadar (yeniden giriş veya `RefreshSignInAsync`) eski ad görünür.

---

### Admin formlarının DTO'ları
Giriş, şifre değiştirme ve Hesabım sayfasının veri sınıfları da diğer DTO'lar gibi **DtoLayer**'da (`Dtos/AccountDtos`):
- `LoginUserDto`: giriş formu (Email, Password, ReturnUrl).
- `ChangePasswordDto`: şifre değiştirme formu (CurrentPassword, NewPassword, ConfirmPassword).
- `ResultAccountDto`: Hesabım sayfasına giden veri (ad, e-posta, şifre formu, hata listesi).

Panelin "En çok satanlar" satırı da `Dtos/OrderDtos/ResultBestSellerDto`. Admin sayfalarının para/tarih/durum biçimlendirmesini yapan `AdminDisplay` ise bir DTO değil, bir görünüm yardımcısı; müşteri tarafındaki `Display` ile birlikte `WebUI/Models` klasöründe. Böylece `Areas/Admin` altında sadece `Controllers`, `ViewComponents` ve `Views` var.

Bu sınıflarda `[Required]`, `[EmailAddress]`, `[Compare]` gibi attribute'lar kullanılıyor. Bunlar `System.ComponentModel.DataAnnotations` namespace'inden geliyor ve .NET'in içinde olduğu için DtoLayer'a ek paket gerekmiyor.

## 4. AccountController ve AccountService

**İş bölümü:** Giriş ve çıkış **çerezle** ilgili olduğu için web katmanında, `SignInManager` ile yapılır. Hesabım sayfasının verisi ve şifre değiştirme ise BusinessLayer'daki `AccountService`'te (Identity'nin `UserManager`'ı ile).

```csharp
[Route("admin")]
public class AccountController : AdminControllerBase
```
Base sınıftan gelen `[Authorize]` nedeniyle kilitli. Giriş sayfası herkese açık olmak **zorunda**, bu yüzden o action'larda `[AllowAnonymous]` var.

### GET login (`LoginPage`)
```csharp
[AllowAnonymous]
[HttpGet("login")]
public IActionResult LoginPage([FromQuery] LoginUserDto input)
{
    if (User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole(AppRoles.Admin))
        return LocalRedirect(SafeReturnUrl(input.ReturnUrl));   // zaten girişliyse formu gösterme
    ModelState.Clear();
    return View("Login", input);
}
```
- `?ReturnUrl=/admin/orders` adres çubuğundan **`LoginUserDto`'ya** doluyor (`[FromQuery]`); controller nesne oluşturmuyor.
- `ModelState.Clear()`: DTO'daki `[Required]` kuralları GET'te boş e-posta/şifre için hata üretir; form ilk açıldığında hata görünmesin diye temizleniyor.
- Metodun adı `LoginPage`, çünkü POST metodu da `LoginUserDto` alıyor ve C#'ta aynı ad + aynı parametre tipiyle iki metot olamaz. Adres `[HttpGet("login")]` ile belirlendiği için yine `/admin/login`; view adı açıkça (`"Login"`) veriliyor.

### POST login
```csharp
[AllowAnonymous]
[HttpPost("login")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Login(LoginUserDto input)
{
    if (!ModelState.IsValid) return View(input);
    string email = input.Email!.Trim();
    var result = await _signInManager.PasswordSignInAsync(email, input.Password!, true, true);
    ...
}
```
**ModelState ve Data Annotations:** `LoginUserDto`'daki (DtoLayer/Dtos/AccountDtos) `[Required]` ve `[EmailAddress]` kuralları model binding sırasında kendiliğinden kontrol edilir; sonuç `ModelState.IsValid`.

`PasswordSignInAsync(userName, password, isPersistent, lockoutOnFailure)`:
- `isPersistent: true` → kalıcı çerez (tarayıcı kapansa da 30 gün).
- `lockoutOnFailure: true` → yanlış şifrede sayaç artar; 5'e ulaşınca hesap 5 dakika kilitlenir.

Giriş başarılıysa kullanıcının **Admin rolünde** olup olmadığına da bakılır; değilse hemen çıkış yaptırılır. Hata mesajları:
- Kilitliyse "Çok fazla hatalı deneme yapıldı...".
- Değilse **"E-posta veya şifre hatalı."**: hangisinin yanlış olduğu bilerek söylenmiyor. "Bu e-posta kayıtlı değil" deseydi saldırgan hangi e-postaların kayıtlı olduğunu öğrenebilirdi (**user enumeration**).
- `ModelState.AddModelError("", ...)`: belirli bir alana değil forma genel hata. `Login.cshtml` bütün hataları `ViewData.ModelState.Values.SelectMany(v => v.Errors)` ile toplayıp gösteriyor.
- `input.Password = null`: hatalı şifre sayfanın HTML'ine geri yazılmasın.

### SafeReturnUrl: dört katmanlı kontrol
```csharp
if (string.IsNullOrEmpty(returnUrl)) return "/admin";
if (!Url.IsLocalUrl(returnUrl)) return "/admin";                                          // dış site yok
if (!returnUrl.StartsWith("/admin", StringComparison.OrdinalIgnoreCase)) return "/admin";  // sadece admin
if (returnUrl.StartsWith("/admin/login", StringComparison.OrdinalIgnoreCase)) return "/admin"; // döngü yok
return returnUrl;
```

### Logout: neden POST?
GET olsaydı başka bir site `<img src=".../admin/logout">` ekleyerek seni sessizce çıkış yaptırabilirdi. Durumu değiştiren her işlem POST + antiforgery olmalı.

### Hesabım (`Index`) ve şifre değiştirme (`ChangePassword`)
```csharp
[HttpGet("account")]
public async Task<IActionResult> Index()
{
    var account = await _accountService.GetAccountAsync(_userManager.GetUserId(User));
    if (account == null) return Challenge();
    return View(account);
}

[HttpPost("account/password")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ChangePassword([Bind(Prefix = "Password")] ChangePasswordDto input)
{
    var result = await _accountService.ChangePasswordAsync(_userManager.GetUserId(User), input);
    if (result == null) return Challenge();
    if (!result.Succeeded) return View("Index", result);
    var user = await _userManager.GetUserAsync(User);
    if (user != null) await _signInManager.RefreshSignInAsync(user);
    return View("Index", result);   // Succeeded = true → sayfa "Şifreniz değiştirildi." yazar
}
```
- `_userManager.GetUserId(User)`: giriş çerezinden kullanıcının Id'sini okur.
- **`[Bind(Prefix = "Password")]`:** Hesabım sayfasındaki şifre formunun alan adları `Password.CurrentPassword`, `Password.NewPassword` gibi önekli yazılmış. Action sadece `ChangePasswordDto` istediği için önekin atlanması gerektiğini bu attribute söylüyor.
- **`AccountService.ChangePasswordAsync`:** önce DTO'daki kuralları (`[Required]`, `[StringLength]`, `[Compare]`) `Validator.TryValidateObject` ile kontrol eder; sonra `UserManager.ChangePasswordAsync` ile mevcut şifreyi doğrulayıp yenisini kaydeder. Hatalar Türkçe olarak `Errors` listesine yazılır (`PasswordMismatch` → "Mevcut şifreniz hatalı."), başarıda `Succeeded = true`.
- `RefreshSignInAsync`: şifre değişince güvenlik damgası değişir ve eski çerezler geçersizleşir; bu tarayıcının oturumu açık kalsın diye çerez yenileniyor.

---

## 5. DashboardController

```csharp
[HttpGet("")]
public async Task<IActionResult> Index()
{
    return View(await _orderService.GetDashboardAsync());
}
```
Controller tek satır. Bütün rakamlar `OrderService.GetDashboardAsync` içinde hesaplanıp `ResultDashboardDto` olarak dönüyor:
```csharp
dashboard.TodayOrders  = orders.Count(o => o.CreatedAt >= today);
dashboard.TodayRevenue = notCancelled.Where(o => o.CreatedAt >= today).Sum(o => o.Total);
dashboard.WeekRevenue  = notCancelled.Where(o => o.CreatedAt >= weekStart).Sum(o => o.Total);   // bugün dahil son 7 gün
dashboard.OpenOrders   = orders.Count(o => o.Status == Received || o.Status == Preparing || o.Status == Ready);
foreach (OrderStatus status in Enum.GetValues<OrderStatus>())
    dashboard.StatusCounts[status] = orders.Count(o => o.Status == status);
dashboard.BestSellers = notCancelled
    .SelectMany(o => o.Details)          // bütün siparişlerin bütün kalemleri tek liste
    .GroupBy(d => d.ProductName)         // ürün adına göre grupla
    .Select(g => new ResultBestSellerDto { Name = g.Key, Quantity = g.Sum(d => d.Quantity), Revenue = g.Sum(d => d.LineTotal) })
    .OrderByDescending(r => r.Quantity)
    .Take(5)
    .ToList();
dashboard.LatestOrders = orders.Take(6).ToList();
```
- İptal edilen siparişler sayılıyor ama **ciroya katılmıyor**.
- `Enum.GetValues<T>()`: enum'un bütün değerleri; yeni bir durum eklenirse panele kendiliğinden gelir.
- En çok satanlar LINQ ile tek ifadede: `SelectMany` (düzleştir) → `GroupBy` (grupla) → `Select` (her grup için bir satır) → sırala → ilk 5.
- Hesaplar RAM'de yapılıyor. Az veride sorun değil; çok veride SQL'e taşınmalı.

View'ın modeli `ResultDashboardDto`; `ViewBag` kullanılmıyor.

---

## 6. OrdersController

### Index: filtre + arama + sayfalama
```csharp
[HttpGet("")]
public async Task<IActionResult> Index(OrderFilterDto filter)
{
    return View(await _orderService.GetAdminListAsync(filter));
}
```
- `?status=Received&channel=Table&q=CF-12&page=2` adres çubuğundan **`OrderFilterDto`'ya** doluyor (model binding metni enum'a da çeviriyor; geçersiz değer `null` olur).
- `OrderService.GetAdminListAsync`:
  1. Üstteki kartlar için her durumdaki sipariş sayısı (**filtreden önce**).
  2. Durum, kanal ve arama filtresi. Arama sipariş no, müşteri adı veya telefonda; büyük/küçük harf fark etmez.
  3. **Sayfalama:** `TotalPages = Math.Max(1, (int)Math.Ceiling(count / 10.0))`; 23 sipariş → 3 sayfa. `(double)` dönüşümü önemli: `23 / 10` tam sayı bölmesi olup 2 verirdi. Sayfa numarası `Math.Clamp` ile 1 ile son sayfa arasına sıkıştırılır (`?page=99` son sayfayı gösterir). `Skip((page - 1) * 10).Take(10)`.
  4. Sonuç `ResultOrderListDto`: bu sayfanın siparişleri, durum sayıları, filtre, toplam ve sayfa sayısı.
- **Specification deseni ile ilişkisi:** Case'teki "tarih, durum, tutar ve kanala göre tekrar kullanılabilir filtre kuralları" maddesinin karşılığı bu filtreler. Specification uygulansaydı her biri bir sınıf olur, birleştirilebilir olur ve SQL'e çevrilirdi.

### Detail: State'in arayüz tarafı
```csharp
var order = await _orderService.GetByIdAsync(id);
if (order == null) return NotFound();
return View(order);
```
İzinli sonraki durumları artık **servis** hesaplıyor ve DTO'ya koyuyor:
```csharp
// OrderService.GetByIdAsync
dto.AllowedNextStatuses = OrderState.For(order.Status).GetAllowedNext();
```
View sadece bu listedeki durumların düğmesini çiziyor:
```razor
@{ List<OrderStatus> allowedNext = order.AllowedNextStatuses; }
@foreach (OrderStatus target in allowedNext)
{
    <button type="submit" name="target" value="@target" ...>@AdminDisplay.ActionLabel(target)</button>
}
```
- "Çok düğmeli tek form" hilesi: tıklanan düğmenin `target` değeri gönderiliyor.
- İptal düğmesinde `onclick="return confirm('Bu sipariş iptal edilsin mi?');"`.
- Liste boşsa "Bu siparişin durumu Teslim Edildi; artık değiştirilemez." yazar.

### ChangeStatus
```csharp
try
{
    await _orderService.ChangeStatusAsync(id, target, note);
    return Redirect("/admin/orders/" + id);
}
catch (BusinessException ex)
{
    var order = await _orderService.GetByIdAsync(id);
    if (order == null) return NotFound();
    ViewBag.Error = ex.Message;
    return View("Detail", order);
}
```
İzin kontrolü (State) serviste. Başarıda detay sayfasına yönlendirilir; yeni durum ve geçmiş kaydı sayfada görünür. Düğmesi olmayan bir geçiş zorlanırsa detay sayfası doğrudan sarı uyarıyla çizilir: 'Sipariş "Hazırlanıyor" durumundan "Teslim Edildi" durumuna geçemez.'

---

## 7. CategoriesController / ProductsController / TablesController

Üçü de aynı CRUD kalıbını izliyor ve hiçbirinde `IMapper`, `new` veya "ekle mi güncelle mi" kararı yok:

| Action | HTTP | Controller ne yapar | Servis metodu |
|---|---|---|---|
| `Index` | GET `/admin/x` | listeyi sayfaya verir | `GetAllAsync` / `GetAdminListAsync(category, q)` |
| `Create` | GET `/admin/x/new` | boş formu sayfaya verir | `GetFormAsync(null)` → Id = 0, aktif |
| `Edit` | GET `/admin/x/{id}/edit` | dolu formu verir; yoksa 404 | `GetFormAsync(id)` |
| `Save` | POST `/admin/x/save` | formu (UpdateXDto) servise verir | `SaveAsync(dto)`: Id 0 → ekle, değilse güncelle |
| `ChangeStatus` | POST `/admin/x/{id}/status` | aktif/pasif | `ChangeStatusAsync` |
| `Delete` | POST `/admin/x/{id}/delete` | sil; kural ihlalinde listeyi uyarıyla doğrudan çizer | `DeleteAsync` |

Örnek (kategori):
```csharp
[HttpGet("new")]
public async Task<IActionResult> Create() => View("Form", await _categoryService.GetFormAsync(null));

[HttpPost("save")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Save(UpdateCategoryDto form)
{
    try
    {
        await _categoryService.SaveAsync(form);
        return RedirectToAction("Index");
    }
    catch (BusinessException ex)
    {
        ViewBag.Error = ex.Message;
        return View("Form", form);     // redirect YOK: yazılanlar kaybolmasın
    }
}
```
Servis tarafında (`CategoryService`):
```csharp
public async Task<UpdateCategoryDto> GetFormAsync(int? id)
{
    if (id == null) { UpdateCategoryDto form = new UpdateCategoryDto(); form.IsActive = true; return form; }
    var category = await GetEntityAsync(id.Value);     // yoksa BusinessException → controller 404
    return _mapper.Map<UpdateCategoryDto>(category);         // entity → form DTO
}

public async Task SaveAsync(UpdateCategoryDto dto)
{
    if (dto.Id == 0) await CreateAsync(_mapper.Map<CreateCategoryDto>(dto));
    else await UpdateAsync(dto);
}
```
- **"Tek form" hilesi:** Form her zaman `UpdateXDto` ile açılıyor; `Id = 0` yeni kayıt demek. Tek bir `Form.cshtml` hem ekleme hem düzenleme yapıyor.
- Ürün formundaki ve filtre çubuğundaki kategori açılır listesini `CategorySelectViewComponent` çiziyor (`<vc:category-select name="CategoryId" selected="@Model.CategoryId" element-id="category" css-class="adm-select w-100" />`). Eskiden `ProductsController` beş ayrı yerde `ViewBag.Categories` dolduruyordu (Index, Create, Edit, Save hata dalı ve ShowListWithErrorAsync); biri unutulsa liste boş kalırdı. Artık controller kategori servisine hiç bağlı değil.
- **Silme neden POST?** GET bir link olurdu; tarayıcı eklentileri veya botlar linkleri "ziyaret edip" veriyi silebilirdi. Silme formlarında `onsubmit="return confirm(...)"` var.
- `TablesController.Index`: liste ve form aynı sayfada. `?edit=3` verilirse form o masayla dolar; `TableService.GetFormAsync(null)` yeni masa için "en büyük numara + 1" önerir.
- `Regenerate`: kart kaybolunca yeni QR üretir, eski kart çalışmaz.
- `CustomersController` salt okunur: `Index(string? q)` → `GetAllAsync(q)`.
- `NotificationsController.Index` → `NotificationService.GetPageAsync(target, unread)` → `ResultNotificationPageDto` (liste + hedef başına okunmamış sayıları). `MarkRead` ve `ReadAll` okunmamışları okundu yapıp geri döner.

**Silme formlarındaki antiforgery:** Formlar `action="/admin/categories/@c.Id/delete"` gibi düz adres kullanıyor. Form Tag Helper, site içi (göreli) adresli POST formlarına da antiforgery token ekliyor.

---

## 8. `_AdminLayout` ve admin View Component'leri

```razor
@{
    var page = ViewData["AdminPage"] as string ?? "";
    var userName = User.FindFirst(AppClaimsPrincipalFactory.FullNameClaim)?.Value ?? User.Identity?.Name ?? "Admin";
}
...
@await Component.InvokeAsync("AdminSidebar", new { activePage = page })
...
<strong>@ViewData["Crumb"]</strong>                         @* breadcrumb *@
@await Component.InvokeAsync("AdminNotifications")           @* zil *@
...
@RenderBody()
<script src="~/js/admin-shell.js" asp-append-version="true"></script>
```
- `User`: view'larda da erişilebilen `ClaimsPrincipal`.
- `?.` ve `??` zinciri: claim yoksa → kullanıcı adı → "Admin".
- `InvokeAsync("AdminSidebar", new { activePage = page })`: anonim nesnedeki property adı, view component'in `InvokeAsync(string activePage)` parametresiyle eşleşiyor.

### Admin partial'ları ve CategorySelectViewComponent

| Parça | Tür | Model | Ne çizer |
|---|---|---|---|
| `Dashboard/_StatCards` | partial | `ResultDashboardDto` | 4 sayı kartı |
| `Dashboard/_LatestOrders`, `_BestSellers`, `_StatusCounts` | partial | `ResultDashboardDto` | Son siparişler, en çok satanlar, durum sayıları |
| `Orders/_OrderItems` | partial | `ResultOrderDto` | Sipariş satırları ve toplamlar |
| `Orders/_OrderHistory` | partial | `ResultOrderDto` | Durum geçmişi |
| `Orders/_OrderStatusForm` | partial | `ResultOrderDto` | **State**: `AllowedNextStatuses`'tan durum butonları |
| `Orders/_OrderCustomer` | partial | `ResultOrderDto` | Masa / müşteri bilgileri |
| `Shared/_ErrorMessage` | partial | `string?` | Kural hatası kutusu |
| `CategorySelectViewComponent` | View Component | `List<ResultCategoryDto>` | Kategori `<select>`'i (ürün filtresi + ürün formu) |

`CategorySelectViewComponent.InvokeAsync(string name, int? selected, string? emptyText = null, string? elementId = null, string cssClass = "adm-select")`: varsayılan değerli parametreler `<vc:...>` etiketinde yazılmayabilir. Filtrede `empty-text="Tüm Kategoriler"` verilir (en üste boş seçenek), formda verilmez (ürünün mutlaka kategorisi olmalı).

### AdminSidebarViewComponent
```csharp
public async Task<IViewComponentResult> InvokeAsync(string activePage)
{
    ViewBag.ActivePage = activePage;
    ViewBag.CategoryCount = (await _categoryService.GetAllAsync()).Count;
    ViewBag.ProductCount  = (await _productService.GetAllAsync()).Count;
    ViewBag.NewOrderCount = await _orderService.CountByStatusAsync(OrderStatus.Received);
    ViewBag.UnreadCount   = await _notificationService.CountUnreadAsync(NotificationTarget.Admin);
    string? fullName = UserClaimsPrincipal.FindFirst(AppClaimsPrincipalFactory.FullNameClaim)?.Value;
    ViewBag.UserName = fullName ?? "Admin";
    return View();
}
```
Ekran görüntüsündeki rozetler: Categories **8**, Products **20**, Orders **1** (yeni), Notifications **18**.

**Verimsizlik:** Sadece sayıları göstermek için bütün kategoriler (ürünleriyle birlikte!) ve bütün ürünler çekiliyor. Bu her admin sayfasında oluyor. `_categoryRepository.CountAsync()` gibi bir `CountAsync` metodu servise eklenirse tek bir `SELECT COUNT(*)` yeterli olurdu. Bu, "çalışıyor ama ölçeklenmez" kodu fark etmek için güzel bir örnek.

### AdminNotificationsViewComponent
```csharp
ViewBag.UnreadCount = await _notificationService.CountUnreadAsync(NotificationTarget.Admin);
var latest = await _notificationService.GetLatestAsync(NotificationTarget.Admin, 6);
return View(latest);
```
Üst çubuktaki zil: son 6 admin bildirimi. Observer'ın (`AdminObserver`) yazdıklarını kullanıcıya gösteren yer burası. Desenin "gözle görülen" çıktısı.

### `AdminDisplay`: admin view yardımcıları
```csharp
Money(amount)          → "₺1,250.50"
Date(value)            → "12 Nov 2026, 20:50"
StatusBadge(status)    → "adm-badge-info" / warning / success / neutral / inactive   (renk)
ActionLabel(target)    → "Start preparing" / "Mark as ready" / "Mark as delivered" / "Cancel order"
ActionIcon(target)     → bi-fire / bi-bell / bi-check2-all / bi-x-circle
ActionButtonClass      → iptal kırmızı, diğerleri ana renk
ChannelLabel / ChannelIcon
Who(order)             → masa siparişinde "Masa 7 · Bahçe Köşesi", diğerlerinde müşteri adı
Initials("Selin Aydın") → "SA"
```
`Initials`: `Split(' ', StringSplitOptions.RemoveEmptyEntries)` → `["Selin", "Aydın"]` → ilk ve son kelimenin baş harfi. Tek kelimeyse sadece ilk harf, boşsa "?".

Neden view içinde `@if` zincirleri değil de statik bir sınıf? Aynı eşleme (durum → renk) birden fazla sayfada kullanılıyor: liste, detay, dashboard. Tek yerde dururlar.

**Bir gözlem:** `ActionLabel` aslında durum başına davranışa benziyor ("bu duruma geçiş düğmesinin adı"). Klasik State uygulansaydı bunlar durum sınıflarının birer property'si olabilirdi (`ReadyState.ActionLabel = "Mark as ready"`). Ama bu metinler **sunum** bilgisi; BusinessLayer'a koymak katman ayrımını bozardı. WebUI'da durmaları doğru.

---

## Kendini sına
1. Yeni bir admin controller eklerken `[Authorize]` yazmayı unutursan ne olur? Neden?
2. Giriş hatasında neden "bu e-posta kayıtlı değil" denmiyor?
3. Logout ve Delete neden GET değil?
4. User Secrets'taki admin şifresini değiştirmek mevcut admin'in şifresini neden değiştirmiyor?
5. `[Bind(Prefix = "Password")]` olmasaydı `ChangePassword` action'ı formdaki değerleri neden alamazdı?
6. Sidebar'daki sayılar her sayfada kaç SQL sorgusu üretiyor? Nasıl azaltırdın?
7. Admin sipariş listesindeki filtreleri Specification deseniyle nasıl yazardın?
