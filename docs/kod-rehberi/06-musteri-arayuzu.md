# 06 · Müşteri arayüzü: controller'lar, Session sepeti, view'lar

> Bu bölüm WebUI'ın müşteri tarafını kapsıyor: HomeController, ProductsController, CartController, Session sepeti, View Component'ler, Razor layout'u ve view'lar, ve JavaScript'in (neredeyse hiçbir şey yapmayan) rolü.

---

## 0. Bu arayüzün temel felsefesi

README'deki cümle: *"JavaScript hiçbir işlem yapmaz: veri getirmez, form göndermez, fiyat hesaplamaz."*

Yani bu klasik, **sunucu tarafında çizilen** bir MVC uygulaması:
- Her tıklama ya bir link (GET) ya da bir form gönderimi (POST).
- Sunucu sayfayı baştan çizer.
- AJAX, fetch, React gibi şeyler yok.

Avantajları: bütün mantık C#'ta, tek yerde ve test edilebilir. Fiyat gibi kritik bir şey tarayıcıda hesaplanmıyor. Dezavantajı: her adımda sayfa yeniden yükleniyor (örneğin adet + / – butonları).

---

## 1. Post/Redirect/Get (PRG) deseni ve TempData

Neredeyse her POST action'ı şu kalıbı izliyor:
```csharp
[HttpPost("add")]
public async Task<IActionResult> Add(...)
{
    // işi yap
    TempData["CartToast"] = product.Name + " sepete eklendi";
    return RedirectBack(returnUrl);       // ← 302 ile GET sayfasına dön
}
```
**Neden doğrudan `View()` döndürülmüyor?** POST'un cevabı bir sayfa olsaydı, kullanıcı F5'e bastığında tarayıcı "formu tekrar göndereyim mi?" diye sorardı. Evet derse ürün sepete **ikinci kez** eklenir, ya da sipariş **ikinci kez** verilirdi. PRG'de POST her zaman bir yönlendirmeyle biter; F5 sadece GET'i tekrarlar ve zararsızdır.

**TempData:** Yönlendirmeden sonra mesajı taşımanın yolu. `ViewBag` sadece o istekte yaşar ve redirect'te kaybolur. `TempData` bir **sonraki** isteğe kadar yaşar ve okunduğu anda silinir. ASP.NET Core'da varsayılan olarak şifrelenmiş bir **çerezde** saklanır (`CookieTempDataProvider`). Session'da saklanması için `AddSessionStateTempDataProvider()` gerekirdi; bu projede o ayar yok. Toast mesajı ve "sepet panelini aç" bayrağı (`OpenCart`) bu şekilde taşınıyor.

**İstisna:** `Checkout` hata olduğunda redirect **yapmıyor**, `return View("Index", cartView)` diyor. Neden? Kullanıcının yazdığı ad, telefon ve adres kaybolmasın diye. Bu bilinçli bir tercih (aşağıda ayrıntısı var).

## 2. Antiforgery (CSRF koruması)

Her POST action'ında `[ValidateAntiForgeryToken]` var. Form tarafında ise `asp-action` / `asp-controller` kullanan her `<form method="post">` **Form Tag Helper** sayesinde otomatik olarak gizli bir alan alıyor:
```html
<input name="__RequestVerificationToken" type="hidden" value="CfDJ8If1qQ..." />
```
Saldırı senaryosu: kötü bir site, gizli bir formla senin tarayıcından `/cart/checkout`'a POST gönderiyor. Tarayıcı çerezleri otomatik eklediği için istek "senden" geliyormuş gibi görünür. Ama kötü sitenin token'ı bilmesi mümkün değil, bu yüzden istek 400 ile reddedilir.

Bugünkü testte bunu bizzat yaşadık: boş bir oturumla `/cart`'tan token almaya çalıştık. Sepet boşken o sayfada form olmadığı için token gelmedi ve POST **400** döndü ("Hay aksi, bir aksilik oldu"). Koruma çalışıyor.

`_ViewImports.cshtml`'deki `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers` satırı bütün tag helper'ları açıyor.

---

## 3. HomeController

```csharp
[HttpGet("/")]
public IActionResult Index()
{
    return View();     // controller veri getirmiyor
}
```
- `[HttpGet("/")]`: başında `/` olduğu için controller'ın route ön ekinden bağımsız, mutlak bir adres.
- **Controller veri getirmiyor ve servis almıyor.** Ana sayfa tamamen View Component'lerden oluşuyor; veri gereken bölümler veriyi **kendisi** getiriyor:
  - `_HomeCategoryShowcase` → `CategoryService.GetActiveWithProductsAsync()` ("ürünü olmayan kategoriyi gösterme" kuralı o serviste)
  - `_HomeFeaturedProducts` → `ProductService.GetFeaturedAsync(6)` (rozetli öne çıkan ürünler)
- Eskiden controller öne çıkan ürünleri model olarak veriyordu (`View(await _productService.GetFeaturedAsync(6))`), kategorileri ise `ViewBag.Categories` ile. Artık her bölüm kendi verisinden sorumlu; ana sayfaya yeni bir veri bölümü eklemek controller'a dokunmayı gerektirmiyor.
- Statik bölümler: `_HomeHero`, `_HomeOrderWays`, `_HomeAbout`, `_HomeOffer`, `_HomeReviews`. `_HomeOffer` bir "latte + pasta" önerisidir; indirim kodu yoktur, gösterilen ₺385 iki ürünün normal fiyatlarının toplamıdır.

`Views/Home/Index.cshtml` artık sadece bölümlerin sırası:
```razor
@await Component.InvokeAsync("_HomeHero")
@await Component.InvokeAsync("_HomeCategoryShowcase")
@await Component.InvokeAsync("_HomeFeaturedProducts")
@await Component.InvokeAsync("_HomeOrderWays")
@await Component.InvokeAsync("_HomeAbout")
@await Component.InvokeAsync("_HomeOffer")
@await Component.InvokeAsync("_HomeReviews")
```

```csharp
[HttpGet("/error")]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public IActionResult Error()
{
    string requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
    return View("Error", requestId);
}
```
Hata sayfası asla önbelleğe alınmamalı. Model sadece bir metin: loglarla eşleştirmek için istek numarası (view'da `@model string?`). `View("Error", requestId)` yazılıyor, çünkü `View(requestId)` yazılsaydı MVC metni **view adı** sanardı. Sitede bu sayfaya giden bir link yok; `Program.cs`'teki `app.UseExceptionHandler("/error")` yakalanmamış bir hata olunca (sadece Development dışında) buraya yönlendiriyor. View `Views/Home/Error.cshtml`'de değil, `Views/Shared/Error.cshtml`'de; MVC önce `Home`, bulamazsa `Shared` klasörüne bakıyor.

```csharp
[Route("/status/{code:int}")]
public IActionResult Status(int code)
{
    Response.StatusCode = code;
    return View(code);
}
```
`UseStatusCodePagesWithReExecute("/status/{0}")` buraya geliyor (ör. `/products/9999` → 404 sayfası). `[HttpGet]` değil `[Route]`, çünkü hatalı bir POST da buraya yeniden çalıştırılabilir. `Response.StatusCode = code` olmasaydı hata sayfası "200 OK" ile dönerdi.

---

## 4. ProductsController

```csharp
[Route("products")]
public class ProductsController : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? category, string? q)
```
**Model binding:** `/products?category=pastalar&q=latte` → `category = "pastalar"`, `q = "latte"`. ASP.NET parametre adlarını query string'deki adlarla eşleştiriyor. Parametre yoksa `null`.

```csharp
        var products = await _productService.GetMenuAsync(category, q);
        ViewBag.Category = category;
        ViewBag.Search = q;
        return View(products);
    }
```
Kategori düğmelerini `_MenuCategoryFilterViewComponent` çiziyor (`@await Component.InvokeAsync("_MenuCategoryFilter", new { selected = selected })`); controller ViewBag'e sadece **seçili filtreyi** yazıyor, kategori listesini değil. Filtre ve arama **sunucuda**. View'da arama kutusu `<form method="get" action="/products">`: GET formu, yani değerler adres çubuğuna yazılıyor. Sayfayı yer imlerine ekleyebilir veya linki paylaşabilirsin. Kategori seçiliyken arama yapılınca kategori kaybolmasın diye formda gizli bir `<input name="category">` var.

```csharp
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var product = await _productService.GetActiveByIdAsync(id);
        if (product == null)
        {
            Response.StatusCode = 404;
            return View("NotFound");        // özel 404 sayfası
        }
        return View(product);
    }
```
Seçenekleri `_ProductDetailOptionsViewComponent` (`@await Component.InvokeAsync("_ProductDetailOptions", new { product = Model })`), benzer ürünleri `_ProductDetailRelatedViewComponent` (`@await Component.InvokeAsync("_ProductDetailRelated", new { productId = Model.Id, categoryId = Model.CategoryId })`) kendisi getiriyor. Controller'ın işi sadece ürünü bulmak.
`return NotFound()` yerine özel bir view döndürülüyor ve status code elle 404 yapılıyor. Böylece genel 404 sayfası yerine "bu ürün bulunamadı" sayfası çıkıyor.

---

## 5. Session sepeti: `Cart/CartSession.cs`

Session web katmanına ait bir şey; bu yüzden onu okuyup yazan küçük yardımcılar WebUI'da duruyor. Hepsi **extension method**: `static` sınıf, `static` metot ve ilk parametrede `this ISession session`. Bu sayede `HttpContext.Session.GetCart()` diye, sanki `ISession`'ın kendi metoduymuş gibi çağrılıyor.

| Metot | Ne yapar |
|---|---|
| `GetCart()` | Session'daki JSON'u `List<CartItemDto>`'ya çevirir; yoksa boş liste |
| `SaveCart(cart)` | Listeyi JSON'a çevirip Session'a yazar |
| `ClearCart()` | Sepeti boşaltır |
| `GetChannel()` / `SetChannel(channel)` | Seçilen sipariş türünü okur/yazar (`Enum.TryParse` ile güvenli okuma) |
| `GetQrToken()` | Okutulan masanın QR kodu |
| `SetTable(qrToken)` | QR kodunu yazar ve sipariş türünü Masa yapar |
| `CartCount()` | Navbar rozeti için toplam adet |

- Session sadece `string`, `int` ve `byte[]` saklayabilir; liste bu yüzden JSON'a çevriliyor (`System.Text.Json`).
- Session'da üç anahtar var: `Cart` (sepet JSON'u), `Channel` (`"Delivery"` gibi), `QrToken` (`"CF-T07-GUL"`).
- Sepet her okunduğunda JSON'dan yeni bir liste oluşur. Listeyi değiştirdikten sonra **`SaveCart` çağrılmazsa değişiklik kaybolur.**

---

## 6. CartController ve CartService

**İş bölümü:**
```text
CartController (WebUI)   →  Session'dan sepeti okur, formdan gelen DTO ile birlikte servise verir,
                            servisin değiştirdiği sepeti Session'a geri yazar, dönen DTO'yu sayfaya verir.
CartService (Business)   →  sepete ekleme kuralları, adet sınırları, sepet sayfasını hazırlama, sipariş verme.
```
Controller'da hiç `new`, hesap veya kural yok.

### Index: sepet sayfası + QR girişi
```csharp
[HttpGet("")]
public async Task<IActionResult> Index(string? qr)
{
    if (qr != null)
    {
        var table = await _tableService.GetByQrAsync(qr);
        if (table == null)
            TempData["CartError"] = "Bu masa QR kodu geçerli değil. ...";
        else
        {
            HttpContext.Session.SetTable(table.QrToken);
            TempData["CartToast"] = "Hoş geldiniz! Masa " + table.Number + " için sipariş verebilirsiniz.";
        }
        return Redirect("/cart");      // QR adres çubuğunda kalmasın
    }
    return View(await GetCartPageAsync(null, TempData["CartError"] as string));
}
```
**QR akışı:** Masadaki QR kod `/cart?qr=CF-T07-GUL` adresini açar. Kod doğrulanır (aktif masa mı?), Session'a veritabanındaki kod ve "Masa" türü yazılır, sonra sade `/cart` adresine yönlendirilir.

### GetCartPageAsync → `CartService.GetCartAsync`
```csharp
private async Task<ResultCartDto> GetCartPageAsync(CreateOrderDto? form, string? error)
{
    var page = await _cartService.GetCartAsync(
        HttpContext.Session.GetCart(), HttpContext.Session.GetChannel(), HttpContext.Session.GetQrToken(), form, error);
    HttpContext.Session.SaveCart(page.Items);
    return page;
}
```
Servis tarafında (`CartService.GetCartAsync`):
- Sepet seçilen türe göre fiyatlandırılır: `_orderPricingService.CalculateAsync(channel ?? OrderChannel.Pickup, cart)`. Tür seçilmemişse ücretsiz kanal gibi hesaplanır; sayfa ücret satırına "—" yazar.
- **Silinmiş ürünler sepetten çıkarılır:** fiyatlandırılmış siparişte karşılığı olmayan satırlar `Items` listesine girmez; controller bu temiz listeyi Session'a geri yazar.
- QR kodu tekrar doğrulanır (masa sonradan pasif yapıldıysa kart gösterilmez).
- Formda yazılanlar (`Form`) ve hata mesajı (`Error`) sayfaya geri verilir.

Sayfanın modeli `ResultCartDto`; `ViewBag` kullanılmıyor.

### Add: sepete ekle
```csharp
[HttpPost("add")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Add(AddToCartDto dto)
{
    List<CartItemDto> cart = HttpContext.Session.GetCart();
    try
    {
        var productName = await _cartService.AddItemAsync(cart, dto);
        HttpContext.Session.SaveCart(cart);
        TempData["CartToast"] = productName + " sepete eklendi";
        TempData["OpenCart"] = true;
    }
    catch (BusinessException) { return NotFound(); }
    return RedirectBack(dto.ReturnUrl);
}
```
**Model binding ile DTO:** Formdaki `productId`, `optionIds` (birden fazla checkbox → liste), `sizeId`, `milkId`, `syrupId` ("Şurupsuz"un değeri boş → `null`), `quantity` ve `returnUrl` alanları ASP.NET tarafından **`AddToCartDto` nesnesine kendiliğinden** doldurulur. Controller nesne oluşturmaz.

Servis tarafında (`CartService.AddItemAsync`):
- Ürün aktif değilse `BusinessException` → controller 404 döner.
- Özelleştirilemeyen üründe gelen seçenekler **atılır** (birinci savunma hattı; ikincisi Chain'deki `ProductHandler`).
- `options.Sort()`: aynı seçim farklı sırayla gelirse (`[5,3]` ve `[3,5]`) aynı satır sayılsın diye.
- `item.OptionIds.SequenceEqual(options)`: listeleri eleman eleman karşılaştırır (`==` referans eşitliğine bakardı).
- Aynı ürün + aynı seçenekler → adet artar; farklı seçenekler → yeni satır. Case'teki "farklı özelleştirmeler ayrı kalem" kuralı.
- Adet `Math.Clamp(..., 1, 20)` ile 1–20 arasına sıkıştırılır.

### RedirectBack: open redirect koruması
```csharp
if (returnUrl != null && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
return Redirect("/cart");
```
`returnUrl` formdan geliyor ve değiştirilebilir. `returnUrl=https://kotu-site.com` gönderilirse ve kontrol yapılmasaydı sitemiz kullanıcıyı başka siteye yönlendirirdi (**open redirect** açığı). `Url.IsLocalUrl` sadece site içi adreslere izin verir.

### Update: adet değiştir / satır sil
```csharp
List<CartItemDto> cart = HttpContext.Session.GetCart();
_cartService.UpdateQuantity(cart, index, quantity);   // 0 veya altı → satır silinir, en fazla 20
HttpContext.Session.SaveCart(cart);
```
View tarafındaki HTML hilesi: aynı formda iki düğme var ve her birinin `name="quantity"` ile farklı `value`'su var. Bir `<button>`'ın name/value'su varsa **sadece tıklanan** düğme gönderilir. JavaScript olmadan +/– yapılıyor; "Kaldır" düğmesi `value="0"` gönderiyor.

### Clear ve Channel
- `Clear` → `HttpContext.Session.ClearCart()`.
- `Channel(OrderChannel channel)` → `HttpContext.Session.SetChannel(channel)`. Form `channel=Delivery` gönderir, model binding metni enum'a çevirir. Sayfa `#orderType` bölümüne kaydırılarak yeniden açılır.

### Checkout: siparişi ver
```csharp
[HttpPost("checkout")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Checkout(CreateOrderDto form)
{
    try
    {
        var order = await _cartService.CheckoutAsync(
            HttpContext.Session.GetCart(), HttpContext.Session.GetChannel(), HttpContext.Session.GetQrToken(), form);
        HttpContext.Session.ClearCart();
        return Redirect("/cart/confirmation/" + order.OrderNo);
    }
    catch (BusinessException ex)
    {
        return View("Index", await GetCartPageAsync(form, ex.Message));
    }
}
```
- Form (`FullName`, `Phone`, `Address`, `PickupTime`, `Note`) model binding ile `CreateOrderDto`'ya dolar.
- `CartService.CheckoutAsync`: sipariş türünü **Session'dan** alır ve formdan geleni ezer (seçilmemişse en sıkı kuralları olan Teslimat), sonra `OrderService.PlaceOrderAsync`'i çağırır. 6 desen orada çalışır (Bölüm 05).
- Başarılı: sepet boşaltılır, onay sayfasına PRG.
- Hatalı: aynı sepet sayfası **yazılanlarla birlikte** ve hata mesajıyla çizilir. Bu bilinçli olarak PRG'nin dışında; redirect yapılsaydı kullanıcının yazdıkları kaybolurdu.
- Sadece `BusinessException` yakalanıyor (Bölüm 04).

### Confirmation
```csharp
var order = await _orderQueryService.GetByOrderNoAsync(orderNo.ToUpper());
if (order == null) return NotFound();
return View(order);
```
Sipariş numarasını bilen herkes bu sayfayı görebilir (giriş yok). Numaranın rastgele olması (Bölüm 05) bu yüzden önemli.

---

## 7. View Component'ler

**View Component**, kendi C# sınıfı olan küçük bir sayfa parçasıdır. DI ile servis alıp veriyi kendisi hazırlayabilir ya da parametre olarak aldığı veriyi çizebilir. (Partial view'dan farkı: partial'ın sınıfı yoktur, çağıranın modelini ve ViewData'sını görür.) Projede **hiç partial view yok**; sayfanın her parçası bir View Component.

### Düzen: sayfa başına klasör, her VC ayrı dosya, `_` ön ekli isim
```text
WebUI/ViewComponents/
  Layout/            _LayoutNavbar, _LayoutFooter, _LayoutCartCount, _LayoutCartDrawer, _LayoutCartToast
  Shared/            _ProductCard                       (3 sayfada kullanılıyor)
  Home_Index/        _HomeHero, _HomeCategoryShowcase, _HomeFeaturedProducts, _HomeOrderWays, _HomeAbout, _HomeOffer, _HomeReviews
  Products_Index/    _MenuHeader, _MenuCategoryFilter, _MenuPromo
  Products_Detail/   _ProductDetailBreadcrumb, _ProductDetailGallery, _ProductDetailOptions, _ProductDetailRelated
  Cart_Index/        _CartHeader, _CartItems, _CartChannelPicker, _CartCheckoutForm, _CartSummary
  Cart_Confirmation/ _CartConfirmationReceipt
WebUI/Areas/Admin/ViewComponents/
  Layout/            _AdminSidebar, _AdminNotifications
  Shared/            _AdminErrorMessage, _AdminCategorySelect
  Dashboard_Index/   _DashboardStatCards, _DashboardLatestOrders, _DashboardBestSellers, _DashboardStatusCounts
  Orders_Detail/     _OrderDetailItems, _OrderDetailHistory, _OrderDetailStatusForm, _OrderDetailCustomer
```
- **Klasör adı = `Controller_Action`:** VC'nin hangi sayfaya ait olduğu klasörden belli (`Home_Index` → `HomeController.Index`). Birden çok sayfada kullanılanlar `Shared`, layout'takiler `Layout` klasöründe.
- **Her VC ayrı dosya:** dosya adı sınıf adıyla aynı (`_HomeAboutViewComponent.cs`).
- **`_` ön eki:** sayfa parçası olduğunu belli ediyor (eskiden partial'lar `_` ile başlardı). İsim = `_` + sayfa + bölüm.

### Örnek: veriyi kendisi getiren VC
```csharp
namespace CafeOrder.WebUI.ViewComponents.Home_Index;

public class _HomeFeaturedProductsViewComponent : ViewComponent
{
    private readonly IProductService _productService;                 // DI'dan (DbContext DEĞİL, servis)

    public _HomeFeaturedProductsViewComponent(IProductService productService)
    {
        _productService = productService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var products = await _productService.GetFeaturedAsync(6);
        return View(products);    // → Views/Shared/Components/_HomeFeaturedProducts/Default.cshtml
    }
}
```
- **Servis, DbContext değil:** VC doğrudan `AppDbContext` alsaydı katmanlı yapı bozulurdu ve iş kuralları atlanırdı (ör. "sadece aktif ürün ve aktif kategori görünsün" kuralı repository'deki `Active()` içinde).
- **Async:** `InvokeAsync` + `await`; veritabanı beklenirken thread bloklanmıyor.

### Çağırma: `@await Component.InvokeAsync("_Ad", new { ... })`
```razor
@await Component.InvokeAsync("_HomeFeaturedProducts")                              @* parametresiz *@
@await Component.InvokeAsync("_ProductCard", new { product = product })            @* parametreli *@
@await Component.InvokeAsync("_ProductDetailRelated", new { productId = Model.Id, categoryId = Model.CategoryId })
```
- **Ad:** sınıf adından `ViewComponent` eki atılır → `_HomeFeaturedProductsViewComponent` → `"_HomeFeaturedProducts"`.
- **Parametreler:** anonim nesnedeki property adları, `Invoke`/`InvokeAsync` metodunun parametre adlarıyla **birebir** eşleşmeli (`productId` ↔ `int productId`).
- **Dikkat: isim ve parametreler metin.** Derleyici `"_HomeFeaturdProducts"` gibi bir yazım hatasını ya da yanlış parametre adını **yakalamaz**; hata ancak sayfa açılınca çıkar (`InvalidOperationException: A view component named ... could not be found`). Bu yüzden VC adını değiştirince bütün `InvokeAsync` çağrılarını aramak gerekir.
- **View'ı nerede?** `Views/Shared/Components/<Ad>/Default.cshtml` (admin VC'leri için önce `Areas/Admin/Views/Shared/Components/<Ad>/` aranır). Klasör adı VC adıyla aynı olmalı: `Components/_HomeFeaturedProducts/Default.cshtml`.

### İki tür VC: hangisi ne zaman?
1. **Veriyi kendisi getiren VC:** Gösterdiği veri sayfanın geri kalanından **bağımsızsa** (kategori kartları, öne çıkanlar, benzer ürünler). Constructor'da servis alır.
2. **Veriyi parametre olarak alan VC:** Sayfanın **modelini paylaşıyorsa**. Sınıfı tek satırdır: `public IViewComponentResult Invoke(ResultCartDto cart) => View(cart);`

Neden hepsi kendi verisini getirmiyor?
- **`_ProductCard`** bir döngüde her ürün için çağrılıyor; kendisi getirseydi hangi ürünü çizeceğini bilemezdi ve 6 ürün için 6 sorgu atardı.
- **Sepet sayfası bölümleri** aynı `ResultCartDto`'yu kullanıyor; her biri sepeti ayrı hesaplasaydı aynı hesap 5 kez yapılırdı ve sipariş hatasında formda yazılanlar kaybolurdu.
- **Admin sipariş detayı bölümleri** aynı siparişi 4 kez okumak zorunda kalırdı.

| Veriyi kendisi getiren VC | Servis çağrısı | Nerede |
|---|---|---|
| `_HomeCategoryShowcase` | `CategoryService.GetActiveWithProductsAsync()` | Ana sayfa kategori kartları |
| `_HomeFeaturedProducts` | `ProductService.GetFeaturedAsync(6)` | Ana sayfa popüler ürünler |
| `_MenuCategoryFilter` | `CategoryService.GetActiveAsync()` | Menü kategori düğmeleri |
| `_ProductDetailOptions` | `ProductService.GetOptionsAsync()` | Detaydaki boy/süt/ekstra (shot, krema)/şurup |
| `_ProductDetailRelated` | `ProductService.GetRelatedAsync(id, kategori, 4)` | Detaydaki benzer ürünler |
| `_LayoutCartCount`, `_LayoutCartDrawer` | Session + `OrderPricingService.CalculateAsync` | Navbar rozeti, sepet paneli |
| `_LayoutCartToast` | `TempData["CartToast"]` | Sepet bildirimi |

| Parametre alan VC | Parametre | Ne çizer |
|---|---|---|
| `_LayoutNavbar`, `_LayoutFooter` | `string navPage` | Üst menü ve alt bilgi; ana sayfada farklı görünür |
| `_HomeHero`, `_HomeOrderWays`, `_HomeAbout`, `_HomeOffer`, `_HomeReviews` | — | Ana sayfanın statik bölümleri |
| `_ProductCard` | `ResultProductDto product` | Ürün kartı (ana sayfa, menü, benzer ürünler) |
| `_MenuHeader`, `_MenuPromo` | — | Menü başlığı ve alt tanıtım kutusu |
| `_ProductDetailBreadcrumb`, `_ProductDetailGallery` | `ResultProductDto product` | Detayın sayfa yolu ve fotoğrafı |
| `_CartHeader` | — | Sepet başlığı |
| `_CartItems`, `_CartChannelPicker`, `_CartCheckoutForm`, `_CartSummary` | `ResultCartDto cart` | Sepetin 3 adımı ve özet |
| `_CartConfirmationReceipt` | `ResultOrderDto order` | Onay sayfasındaki fiş |

### Diğer notlar
- **Navbar'daki sepet sayısı:** `_LayoutCartCount` Session'daki adetleri toplar. Navbar **her sayfada** var; her controller "sepet sayısını ViewBag'e koy" demek zorunda kalmıyor.
- **Sepet paneli:** `_LayoutCartDrawer` fiyatları `OrderPricingService` ile hesaplıyor (Pickup = ücretsiz, çünkü panelde sadece ara toplam var). Layout bunu sadece ana sayfa, menü ve detay sayfasında çağırıyor.
- **`string` model tuzağı:** `_LayoutNavbar`, `_LayoutFooter`, `_LayoutCartToast` modeli metin olan VC'ler. `return View(navPage)` yazılsaydı MVC metni **view adı** sanardı (`View(string viewName)` aşırı yüklemesi seçilir). Bu yüzden `return View("Default", navPage)` yazıldı.
- **Partial'dan farkı:** Partial, çağıranın `ViewData`'sını ve modelini görür; VC ise sadece kendisine verilen parametreleri görür. Navbar sayfa adını layout'tan açıkça alıyor: `@await Component.InvokeAsync("_LayoutNavbar", new { navPage = page })`.
- **Dikkat (yaşadık):** `_MenuCategoryFilter`'a `selected` null gidince VC'ye `""` (boş metin) gelebiliyor; "Tüm Ürünler" düğmesi seçili görünmüyordu. VC içinde `string.IsNullOrEmpty(selected) ? null : selected` ile düzeltildi.
- **Alternatif yazım:** ASP.NET Core VC'leri etiket olarak da çağırmaya izin veriyor (`<vc:product-card product="@product" />`, `_ViewImports`'taki `@addTagHelper *, CafeOrder.WebUI` ile). Bu yazımda parametre adlarını derleyici kontrol eder. Projede eğitimdeki yazımla aynı olsun diye `Component.InvokeAsync` tercih edildi.

---

## 8. Layout ve view dosyaları

### `_ViewStart.cshtml` ve `_ViewImports.cshtml`
```razor
@* _ViewStart *@
@{ Layout = "_Layout"; }
```
Her view'dan önce çalışır: "bütün sayfalar `_Layout` kullansın."

`_ViewImports`: bütün view'lar için ortak `@using` satırları ve tag helper'lar. Bu sayede view'larda `ResultProductDto` veya `Display.Money(...)` tam isim yazmadan kullanılabiliyor.

### `_Layout.cshtml`
```razor
@{
    var openCart = TempData["OpenCart"] is true;
    var page = ViewData["NavPage"] as string ?? "other";
    bool hasDrawer = page == "home" || page == "menu" || page == "detail";
}
...
<title>@ViewData["Title"]</title>
<link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
@await RenderSectionAsync("Styles", required: false)
...
<!body @Html.Raw(ViewData["BodyAttributes"] as string) data-open-cart="@(openCart && hasDrawer ? "true" : null)">
  @await Component.InvokeAsync("_LayoutNavbar", new { navPage = page })
  @RenderBody()
  @await Component.InvokeAsync("_LayoutFooter", new { navPage = page })
  @if (hasDrawer) { @await Component.InvokeAsync("_LayoutCartDrawer") }
  @await Component.InvokeAsync("_LayoutCartToast")
  <script src="~/js/site.js" asp-append-version="true"></script>
  @await RenderSectionAsync("Scripts", required: false)
</!body>
```
- **`ViewData` ile `ViewBag`:** Aynı sözlüğe iki farklı erişim yolu. `ViewData["Title"]` = `ViewBag.Title`. View'lar en üstte `ViewData["Title"] = ...` yazıyor, layout okuyor. Layout'tan **önce** view çalıştığı için bu mümkün.
- `TempData["OpenCart"] is true`: TempData `object` döndürür. `is true` hem null kontrolü hem tip kontrolü yapar.
- `~/css/site.css`: `~` uygulamanın kök dizinidir. `asp-append-version="true"` adresin sonuna dosyanın hash'ini ekler (`site.css?v=abc123`). CSS değiştiğinde tarayıcı eski önbellek yerine yeni dosyayı alır (**cache busting**).
- `RenderSectionAsync("Styles", required: false)`: view'lar `@section Styles { ... }` ile sayfaya özel CSS ekleyebiliyor (örneğin `cart.css`).
- `@RenderBody()`: view'ın kendisi buraya yerleşir.
- `Component.InvokeAsync("_LayoutNavbar", new { navPage = page })`: View Component. Hangi sayfada olduğunu parametre olarak alır (aktif link, ana sayfada şeffaf menü).
- **`<!body ...>`:** Başındaki `!` o eleman için tag helper işlemeyi kapatır. Ana sayfa `ViewData["BodyAttributes"]` ile body'ye Bootstrap scroll-spy özellikleri ekliyor; `@Html.Raw` bu metni HTML olarak (kaçış yapmadan) basıyor. Normalde `@` ile basılan her şey **HTML encode** edilir (`<` → `&lt;`). Bu XSS saldırılarına karşı otomatik bir korumadır. `Html.Raw` bu korumayı kapatır, bu yüzden **sadece sunucunun kendi ürettiği metinle** kullanılmalı (burada öyle).
- `data-open-cart="@(cond ? "true" : null)"`: Razor'da bir attribute'un değeri `null` ise attribute **hiç yazılmaz**. Bu küçük ama kullanışlı bir Razor özelliği.

### `_ProductDetailOptions/Default.cshtml` (ürün detayındaki seçenekler): önemli kısımlar
```razor
@model List<ResultProductOptionDto>
@{
    ResultProductDto product = ViewBag.Product;   // VC InvokeAsync(product) ile verdi
    var options = Model;
    var sizes  = options.Where(o => o.GroupKey == "Size").ToList();
    var milks  = options.Where(o => o.GroupKey == "Milk").ToList();
    var shots  = options.Where(o => o.GroupKey == "ExtraShot").ToList();
    var creams = options.Where(o => o.GroupKey == "Cream").ToList();     // shot gibi checkbox (optionIds)
    var syrups = options.Where(o => o.GroupKey == "Syrup").ToList();
    var startPrice = product.BasePrice;
    if (product.IsCustomizable)
        startPrice += sizes.Where(o => o.IsDefault).Sum(o => o.PriceDelta) + milks.Where(o => o.IsDefault).Sum(o => o.PriceDelta);
}
```
- Seçenek listesi VC'nin **modeli** (`List<ResultProductOptionDto>`), ürün ise `ViewBag.Product` ile geliyor: `ResultProductDto product = ViewBag.Product;` `dynamic`'ten tipli değişkene atama; bu satırdan sonra derleyici yardım edebiliyor.
- Form etiketi (`<form ... asp-action="Add">`) `Products/Detail.cshtml`'de kaldı; VC sadece formun **içindeki** seçenek alanlarını çiziyor. Böylece seçimler aynı formla `/cart/add`'e gidiyor.
- `startPrice`: Varsayılan seçimlerle (Orta +15, Normal Süt 0) başlangıç fiyatı → 160 + 15 = **175**. Ekran görüntüsünde "Başlangıç fiyatı ₺175,00" yazmasının sebebi bu. Bu değer **sadece bilgi amaçlı**; seçim değiştikçe güncellenmiyor (JavaScript yok). Ekranda da *"options are added in the cart"* yazıyor.

```razor
<form method="post" asp-controller="Cart" asp-action="Add">
  <input type="hidden" name="productId" value="@Model.Id" />
  <input type="hidden" name="returnUrl" value="/products/@Model.Id" />
  @foreach (var o in sizes)
  {
    <input type="radio" name="sizeId" value="@o.Id" checked="@o.IsDefault" />
  }
  @foreach (var o in shots)
  {
    <input type="checkbox" name="optionIds" value="@o.Id" />
  }
  <input type="radio" name="syrupId" value="" checked />     @* No syrup *@
  ...
  <input type="number" name="quantity" value="1" min="1" max="20" />
</form>
```
- `asp-controller="Cart" asp-action="Add"` → tag helper `action="/cart/add"` üretir (attribute route'tan) ve antiforgery token ekler.
- `checked="@o.IsDefault"`: Razor, `bool` attribute'larda `false` ise attribute'u hiç yazmaz, `true` ise `checked="checked"` yazar.
- Formda **fiyat alanı yok**, sadece Id'ler var.
- `min="1" max="20"` sadece tarayıcı tarafında bir ipucu; asıl kontrol sunucuda (controller sıkıştırması + `QuantityHandler`).

### `_ProductCard` View Component'i (`Components/_ProductCard/Default.cshtml`): tekrar kullanılan kart
```razor
@model ResultProductDto
...
@if (Model.IsCustomizable)
{
  <a href="/products/@Model.Id" class="btn cf-add-btn">Özelleştir</a>        @* detaya git *@
}
else
{
  <form method="post" asp-controller="Cart" asp-action="Add" class="d-inline">
    <input type="hidden" name="productId" value="@Model.Id" />
    <input type="hidden" name="returnUrl" value="@(Context.Request.Path + Context.Request.QueryString)" />
    <button type="submit">Add</button>                                       @* doğrudan sepete *@
  </form>
}
```
- Özelleştirilebilir ürün kartında "Özelleştir" (detaya gider), diğerlerinde "Ekle" (doğrudan sepete ekler).
- `returnUrl = Path + QueryString`: kullanıcı `/products?category=pastalar` sayfasındaysa ekledikten sonra **aynı filtreli sayfaya** döner.
- Çağrı: `@await Component.InvokeAsync("_ProductCard", new { product = product })`. Sınıf: `ViewComponents/Shared/_ProductCardViewComponent.cs` → `Invoke(ResultProductDto product) => View(product)`.

### `Cart/Index.cshtml`: üç adımlı sayfa
1. **Ürünler:** `@for (var i = 0; ...)` döngüsü. `foreach` değil, çünkü Update formu için `index` gerekli.
2. **Sipariş türü:** radyo düğmeleri + "Continue" → `POST /cart/channel`.
3. **Bilgiler:** sadece `channel != null` ise görünür, alanlar kanala göre değişir:
   - Table: QR geçerliyse masa kartı, değilse "Masa okutmayı dene (demo)" linki (`/cart?qr=CF-T07-GUL`).
   - Delivery: ad, telefon, adres, adres notu.
   - Pickup: ad, telefon, `datetime-local` saat alanı. Değer `@form.PickupTime?.ToString("yyyy-MM-ddTHH:mm")` ile geri dolduruluyor; `?.` null ise hiçbir şey yazmaz.

"Siparişi Ver" düğmesi formun **dışında** (sağdaki özet kutusunda):
```html
<form method="post" asp-action="Checkout" id="checkoutForm"> ... </form>
...
<button type="submit" form="checkoutForm" disabled="@(isEmpty || channel == null)">Siparişi Ver</button>
```
HTML5'in `form="id"` özelliği, düğmeyi uzaktaki bir forma bağlıyor. Tasarımı bozmadan, JavaScript'siz.

Özet kutusundaki ücret satırı:
```razor
@(channel == null ? "—" : Model.DeliveryFee == 0 ? "Free" : Display.Money(Model.DeliveryFee))
```
İç içe ternary: kanal yoksa "—", ücret 0 ise "Free", değilse tutar.

### `Models/Display.cs`: view yardımcıları
```csharp
public static string Money(decimal amount) => "₺" + amount.ToString("N2", Turkish);   // Turkish = tr-TR
public static string BadgeClass(string? badge) { ... "cf-tag-best" ... }
public static string OptionText(string groupKey, string value) { Size → "Büyük boy", ExtraShot → "Ekstra shot", Cream → "Çırpılmış krema", diğerleri olduğu gibi ("Yulaf Sütü", "Vanilya Şurubu") }
public static string ChannelName / ChannelIcon / ChannelText(OrderChannel channel)
```
`N2` = binlik ayırıcı + 2 ondalık → `₺1,000.00`. View'larda tekrar eden biçimlendirme mantığı tek bir yerde toplanmış.

---

## 9. JavaScript: `wwwroot/js/site.js`

```js
(function () {
  "use strict";
  // 1) Kaydırınca navbar'a "scrolled" sınıfı (gölge)
  // 2) <img data-fallback> yüklenemezse yerine ikonlu bir kutu koy
  // 3) Toast'ı 2.6 saniye sonra gizle
  // 4) <body data-open-cart> varsa Bootstrap Offcanvas'ı aç
})();
```
- `(function () { ... })();`: **IIFE** (anında çalışan fonksiyon). Değişkenler global alanı kirletmez.
- `"use strict"`: daha katı JS kuralları (örneğin bildirilmemiş değişkene atama hata verir).
- Dördü de **sadece görsel**. JavaScript kapalıyken bile site tamamen çalışır: sepete ekleme, sipariş verme, admin işlemleri. Buna **progressive enhancement** denir.

Toast konusunda bugün bulduğumuz şey: `.cf-toast` CSS'te `transform: translate(-50%, 140%)` ile ekranın altına itilerek gizleniyor. Tam sayfa ekran görüntüsünde viewport büyüdüğü için ortada boş bir kutu olarak görünüyordu. Bu gerçek kullanımda bir hata değil, ama `visibility: hidden` ile gizlemek daha sağlam olurdu.

`home.js` ve `admin-shell.js` de aynı şekilde sadece görsel işler yapıyor:
- `home.js`: ana sayfadaki `.reveal` bölümleri ekrana girdikçe görünür yapıyor (`IntersectionObserver`; eski tarayıcıda hepsini doğrudan gösteriyor). Ana sayfadaki menü vurgusu ise Bootstrap'in scroll-spy özelliği; `BodyAttributes` ile body'ye eklenen `data-bs-spy` özelliği bunu açıyor.
- `admin-shell.js`: admin sidebar'ını masaüstünde daraltıyor, mobilde açıp kapatıyor.

---

## Kendini sına
1. PRG deseni olmasaydı F5'e basınca ne olurdu? `Checkout` hata durumunda neden PRG'yi bozuyor?
2. `options.Sort()` satırı silinirse hangi senaryoda sepette yinelenen satırlar oluşur?
3. `RedirectBack` içindeki `Url.IsLocalUrl` kontrolü hangi saldırıyı engelliyor?
4. Tek bir formdaki "+" ve "–" düğmeleri JavaScript olmadan nasıl farklı değerler gönderiyor?
5. `CartToast` VC'sinde neden `return View(message)` değil de `return View("Default", message)` yazılıyor?
6. `@Html.Raw` ne zaman tehlikelidir? Layout'taki kullanımı neden güvenli?
7. Checkout'ta `form.Channel` formdan gelse bile neden üzerine yazılıyor?
