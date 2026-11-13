# DTO Denetimi: proje DTO'lar üzerinden mi çalışıyor?

**Kısa cevap: Evet.** Ekran (WebUI) ile iş katmanı (BusinessLayer) arasında taşınan bütün veri DTO'dur. Entity'ler sadece BusinessLayer ile DataAccessLayer arasında dolaşır. Tek istisna Identity'nin kullanıcı sınıfı `AppUser`'dır; ASP.NET Core Identity bu sınıfla çalışmak zorundadır.

**Controller kuralı:** Controller'lar ve view component'ler sadece **DTO alır ve DTO verir**. İçlerinde `new` ile nesne oluşturma, `IMapper`, hesaplama, filtreleme, sayfalama veya "ekle mi güncelle mi" kararı yoktur; bunların hepsi servislerdedir. Formdan ve adres çubuğundan gelen veriyi DTO'ya ASP.NET'in model binding'i doldurur (`AddToCartDto`, `CreateOrderDto`, `UpdateCategoryDto`, `OrderFilterDto`, `LoginUserDto` ...). Kontrol: controller ve view component klasörlerinde `new` kelimesi sadece `[HttpGet("new")]` adres metninde geçer.

```text
WebUI (controller, view)  ──DTO──►  BusinessLayer (servis, desenler)  ──Entity──►  DataAccessLayer (repository, EF Core)
                          ◄──DTO──                                     ◄──Entity──
```

## Nasıl kontrol edildi?

| Kontrol | Sonuç |
|---|---|
| WebUI'da `EntityLayer.Entities` kullanan dosyalar | Sadece 4 dosya ve hepsi `AppUser` için: `Program.cs` (Identity kaydı), `AccountController` (giriş), `AdminAccountSeeder` (ilk admin), `AppClaimsPrincipalFactory` (çereze ad yazma). Başka hiçbir entity WebUI'da kullanılmıyor. |
| WebUI'da repository veya `DbContext` isteyen sınıf | **Yok.** Controller'lar ve view component'ler sadece servis arayüzlerini (`IOrderService` vb.) istiyor. DataAccessLayer sadece `Program.cs`'te kayıt için görünüyor. |
| Servis arayüzlerinin aldığı ve döndürdüğü tipler | Hepsi DTO (`CreateXDto`, `UpdateXDto`, `ResultXDto`, `CartItemDto`, `CreateOrderDto`), enum (`OrderStatus` vb.) veya basit tip (`int`, `bool`, `string`). **Hiçbir servis entity almıyor veya döndürmüyor.** |
| View'ların modelleri (`@model`) | Hepsi DTO veya basit tip: `ResultOrderDto`, `List<ResultProductDto>`, `UpdateCategoryDto`, `LoginUserDto`, `ResultAccountDto`, `int`, `ErrorViewModel`. |
| `ViewBag` ile view'a giden veriler | DTO listeleri, enum'lar, sayılar ve metinler. `ViewBag.Category` ve `ViewBag.Table` sadece değişken adı: içlerinde kategori slug'ı/Id'si ve `ResultTableDto` var. |
| Admin formları | DtoLayer'da: `LoginUserDto`, `ChangePasswordDto`, `ResultAccountDto`, `ResultBestSellerDto`. |
| Sayfa DTO'ları | Birden fazla veri gösteren sayfaların tek bir DTO'su var: `ResultCartDto` (sepet), `ResultDashboardDto` (panel), `ResultOrderListDto` (sipariş listesi), `ResultNotificationPageDto` (bildirimler). |

### Entity'lerin kullanıldığı yer neden doğru?
Desenler (Factory Method, Decorator, Strategy, Chain, State, Observer) BusinessLayer içinde `Order`, `OrderDetail`, `Product` entity'leri üzerinde çalışır. Bu bilinçli bir tercih: EF Core sadece entity'leri tanır ve izler (change tracking). Sipariş tek transaction'da kaydedilirken müşteri, kalemler ve seçeneklerle birlikte bütün nesne grafiği entity olmak zorunda. Servis işini entity'lerle yapar, dışarıya sonucu AutoMapper ile DTO'ya çevirip verir.

### State ve arayüz
Admin sipariş detayındaki izinli durum düğmeleri için gereken liste `OrderService.GetByIdAsync` içinde hesaplanıp `ResultOrderDto.AllowedNextStatuses` olarak geliyor. WebUI State sınıfını hiç tanımıyor.

---

## Servis metotları: ne yapar, ne alır, ne döndürür?

Bütün metotların kodda da Türkçe `/// <summary>` açıklaması var (302 metodun 302'si).

### ICategoryService / CategoryService
| Metot | Ne yapar | Alır | Döndürür |
|---|---|---|---|
| `GetAllAsync` | Admin listesi: bütün kategoriler, ürün sayılarıyla | — | `List<ResultCategoryDto>` |
| `GetActiveAsync` | Müşteri menüsü: aktif kategoriler, aktif ürün sayılarıyla | — | `List<ResultCategoryDto>` |
| `GetActiveWithProductsAsync` | Ana sayfa: ürünü olan aktif kategoriler | — | `List<ResultCategoryDto>` |
| `GetFormAsync` | Admin formu: id null → boş form (Id 0), değilse dolu form | `int?` | `UpdateCategoryDto` |
| `SaveAsync` | Formun kaydet düğmesi: Id 0 → ekle, değilse güncelle | `UpdateCategoryDto` | — |
| `CreateAsync` | Yeni kategori; kod (CAT-009), slug, ikon ve tarih sunucuda üretilir | `CreateCategoryDto` | — |
| `UpdateAsync` | Kategoriyi formdaki değerlerle günceller, slug'ı yeniler | `UpdateCategoryDto` | — |
| `ChangeStatusAsync` | Aktif/pasif yapar | `int`, `bool` | — |
| `DeleteAsync` | Siler; ürünü varsa "hâlâ ürün var" hatası | `int` | — |

### IProductService / ProductService
| Metot | Ne yapar | Alır | Döndürür |
|---|---|---|---|
| `GetMenuAsync` | Müşteri menüsü; kategori slug'ı ve arama ile filtre | `string?`, `string?` | `List<ResultProductDto>` |
| `GetFeaturedAsync` | Ana sayfa: rozetli ilk N ürün | `int` | `List<ResultProductDto>` |
| `GetActiveByIdAsync` | Ürün detay sayfası; pasifse null | `int` | `ResultProductDto?` |
| `GetRelatedAsync` | Aynı kategoriden diğer ürünler | `int`, `int`, `int` | `List<ResultProductDto>` |
| `GetOptionsAsync` | Boyut, süt, shot, şurup seçenekleri | — | `List<ResultProductOptionDto>` |
| `GetAllAsync` | Admin listesi: bütün ürünler | — | `List<ResultProductDto>` |
| `GetAdminListAsync` | Admin listesi, kategori ve ada göre filtreli | `int?`, `string?` | `List<ResultProductDto>` |
| `GetFormAsync` | Admin formu: id null → boş form, değilse dolu form | `int?` | `UpdateProductDto` |
| `SaveAsync` | Kaydet: Id 0 → ekle, değilse güncelle | `UpdateProductDto` | — |
| `CreateAsync` | Yeni ürün (fiyat, rozet, kategori, isim kuralları) | `CreateProductDto` | — |
| `UpdateAsync` | Ürünü günceller | `UpdateProductDto` | — |
| `ChangeStatusAsync` | Aktif/pasif yapar | `int`, `bool` | — |
| `DeleteAsync` | Siler; siparişte geçiyorsa "pasif yapın" hatası | `int` | — |

### ITableService / TableService
| Metot | Ne yapar | Alır | Döndürür |
|---|---|---|---|
| `GetAllAsync` | Bütün masalar, sipariş sayılarıyla | — | `List<ResultTableDto>` |
| `GetFormAsync` | Masa formu: editId → dolu form, değilse yeni masa (numara = en büyük + 1) | `int?` | `UpdateTableDto` |
| `SaveAsync` | Kaydet: Id 0 → ekle, değilse güncelle | `UpdateTableDto` | — |
| `GetByQrAsync` | Okutulan QR aktif bir masaya aitse o masa, değilse null | `string?` | `ResultTableDto?` |
| `CreateAsync` | Yeni masa + rastgele QR kodu | `CreateTableDto` | — |
| `UpdateAsync` | Masayı günceller; numara değiştiyse yeni QR | `UpdateTableDto` | — |
| `RegenerateQrAsync` | Yeni QR üretir, eski kart çalışmaz | `int` | — |
| `ChangeStatusAsync` | Aktif/pasif yapar | `int`, `bool` | — |
| `DeleteAsync` | Siler; siparişi varsa hata | `int` | — |

### ICustomerService / CustomerService
| Metot | Ne yapar | Alır | Döndürür |
|---|---|---|---|
| `GetAllAsync` | Müşteri listesi; ad/telefon araması; sipariş sayısı, harcama, son sipariş | `string?` | `List<ResultCustomerDto>` |

### INotificationService / NotificationService
| Metot | Ne yapar | Alır | Döndürür |
|---|---|---|---|
| `GetAllAsync` | Bildirim listesi; hedef ve okunmamış filtresi | `NotificationTarget?`, `bool` | `List<ResultNotificationDto>` |
| `GetPageAsync` | Bildirimler sayfası: liste + hedef başına okunmamış sayıları | `NotificationTarget?`, `bool` | `ResultNotificationPageDto` |
| `GetLatestAsync` | Üst çubuktaki zil için son N bildirim | `NotificationTarget`, `int` | `List<ResultNotificationDto>` |
| `CountUnreadAsync` | Okunmamış sayısı | `NotificationTarget?` | `int` |
| `MarkAllReadAsync` | Okunmamışları okundu yapar | `NotificationTarget?` | — |

### IOrderService / OrderService
| Metot | Ne yapar | Alır | Döndürür |
|---|---|---|---|
| `GetAllAsync` | Bütün siparişler, en yeni en üstte | — | `List<ResultOrderDto>` |
| `GetAdminListAsync` | Admin listesi: filtre, arama, 10'arlı sayfalama, durum sayıları | `OrderFilterDto` | `ResultOrderListDto` |
| `GetDashboardAsync` | Panel: bugünkü sipariş/ciro, açık sipariş, son 7 gün, durum sayıları, en çok satanlar, son 6 sipariş | — | `ResultDashboardDto` |
| `GetByIdAsync` | Sipariş; kalemleri, seçenekleri, geçmişi ve izinli sonraki durumlarıyla (State) | `int` | `ResultOrderDto?` |
| `GetByOrderNoAsync` | Sipariş numarasıyla (onay sayfası) | `string` | `ResultOrderDto?` |
| `CountByStatusAsync` | Bir durumdaki sipariş sayısı | `OrderStatus` | `int` |
| `CalculateAsync` | Sepeti **kaydetmeden** fiyatlandırır (Decorator + Strategy) | `OrderChannel`, `List<CartItemDto>` | `ResultOrderDto` |
| `PlaceOrderAsync` | Sipariş verir: Factory Method → Decorator + Strategy → Chain → transaction → Observer | `CreateOrderDto`, `List<CartItemDto>`, `string?` (QR) | `ResultOrderDto` |
| `ChangeStatusAsync` | Durum değiştirir: State kontrolü → geçmiş kaydı → Observer | `int`, `OrderStatus`, `string?` | — |
| `CalculateOrderAsync` *(private)* | Asıl hesap: fiyatları veritabanından okur, kalemleri kurar, ara toplam ve ücreti hesaplar | `Order`, `List<CartItemDto>` | — |
| `CalculateUnitPrice` *(private)* | Decorator: taban fiyatı her seçenekle sarar | `Product`, `List<ProductOption>` | `decimal` |
| `GetFeeStrategy` *(private)* | Strategy: kanala uygun ücret sınıfını seçer | `OrderChannel` | `IDeliveryFeeStrategy` |
| `GetCreator` *(private)* | Factory Method: kanala uygun yaratıcıyı seçer | `OrderChannel` | `OrderCreator` |
| `OrdersWithDetails` *(private)* | Siparişi bütün ilişkileriyle getiren ortak sorgu | — | `IQueryable<Order>` |

`private` metotlar entity kullanıyor, ama sınıfın dışına çıkmıyorlar. Dışarıya açık (`public`) olan her metot DTO alıp veriyor.

### ICartService / CartService
Sepetin kendisi Session'da (web katmanı) durduğu için controller sepeti okuyup servise verir, servisin değiştirdiği listeyi geri yazar.

| Metot | Ne yapar | Alır | Döndürür |
|---|---|---|---|
| `AddItemAsync` | Sepete ekler; aynı ürün + aynı seçenekler → adet artar, farklı seçim → yeni satır; adet 1–20; ürün satışta değilse hata | `List<CartItemDto>`, `AddToCartDto` | `string` (ürün adı) |
| `UpdateQuantity` | Satırın adedini değiştirir; 0 veya altı → satırı siler | `List<CartItemDto>`, `int`, `int` | — |
| `GetCartAsync` | Sepet sayfası: fiyatlandırılmış sepet, silinmiş ürünlerden temizlenmiş satırlar, masa, form, hata | `List<CartItemDto>`, `OrderChannel?`, `string?`, `CreateOrderDto?`, `string?` | `ResultCartDto` |
| `CheckoutAsync` | Sipariş türünü Session'daki seçimden alır (yoksa Teslimat) ve siparişi verir | `List<CartItemDto>`, `OrderChannel?`, `string?`, `CreateOrderDto` | `ResultOrderDto` |

### IAccountService / AccountService
Giriş ve çıkış çerezle ilgili olduğu için `AccountController`'da (SignInManager) kalır.

| Metot | Ne yapar | Alır | Döndürür |
|---|---|---|---|
| `GetAccountAsync` | Hesabım sayfası: ad ve e-posta | `string?` (kullanıcı Id) | `ResultAccountDto?` |
| `ChangePasswordAsync` | Form kurallarını (DataAnnotations) ve Identity'yi çalıştırıp şifreyi değiştirir; hatalar Türkçe `Errors`'a | `string?`, `ChangePasswordDto` | `ResultAccountDto?` (`Succeeded`) |

---

## Controller action'ları: ne yapar, servisten ne alır?

### Müşteri tarafı
| Controller.Action | Adres | Ne yapar | Kullandığı DTO |
|---|---|---|---|
| `HomeController.Index` | GET `/` | Ana sayfa: kategoriler + öne çıkan ürünler | `List<ResultCategoryDto>`, `List<ResultProductDto>` |
| `HomeController.Error` | GET `/error` | Beklenmeyen hata sayfası | `string` (istek numarası) |
| `HomeController.Status` | `/status/{kod}` | 404 gibi durum sayfaları | `int` |
| `ProductsController.Index` | GET `/products` | Menü; kategori ve arama filtresi | `List<ResultProductDto>` |
| `ProductsController.Detail` | GET `/products/{id}` | Ürün detayı, seçenekler, ilgili ürünler | `ResultProductDto`, `List<ResultProductOptionDto>` |
| `CartController.Index` | GET `/cart` | Sepet sayfası; `?qr=` ile masa okutma | `ResultCartDto` |
| `CartController.Add` | POST `/cart/add` | Sepete ekler (`CartService.AddItemAsync`) | `AddToCartDto` |
| `CartController.Update` | POST `/cart/update` | Adedi değiştirir / satırı siler (`CartService.UpdateQuantity`) | `CartItemDto` listesi |
| `CartController.Clear` | POST `/cart/clear` | Sepeti boşaltır | — |
| `CartController.Channel` | POST `/cart/channel` | Sipariş türünü Session'a yazar | — |
| `CartController.Checkout` | POST `/cart/checkout` | Siparişi verir (`CartService.CheckoutAsync`); hata olursa sepet sayfası | `CreateOrderDto` → `ResultOrderDto` / `ResultCartDto` |
| `CartController.Confirmation` | GET `/cart/confirmation/{no}` | Sipariş onay sayfası | `ResultOrderDto` |

### Admin tarafı
| Controller.Action | Adres | Ne yapar | Kullandığı DTO |
|---|---|---|---|
| `AccountController.LoginPage` / `Login` | GET / POST `/admin/login` | Giriş formu ve girişi yapma | `LoginUserDto` |
| `AccountController.Logout` | POST `/admin/logout` | Çıkış | — |
| `AccountController.AccessDenied` | GET `/admin/access-denied` | Yetkisiz kullanıcı sayfası | — |
| `AccountController.Index` | GET `/admin/account` | Hesabım sayfası (`AccountService`) | `ResultAccountDto` |
| `AccountController.ChangePassword` | POST `/admin/account/password` | Şifre değiştirme (`AccountService`) | `ChangePasswordDto` → `ResultAccountDto` |
| `DashboardController.Index` | GET `/admin` | Panel (`OrderService.GetDashboardAsync`) | `ResultDashboardDto` |
| `OrdersController.Index` | GET `/admin/orders` | Sipariş listesi (`OrderService.GetAdminListAsync`) | `OrderFilterDto` → `ResultOrderListDto` |
| `OrdersController.Detail` | GET `/admin/orders/{id}` | Sipariş detayı ve izinli durum düğmeleri (`AllowedNextStatuses`) | `ResultOrderDto` |
| `OrdersController.ChangeStatus` | POST `/admin/orders/{id}/status` | Durum değiştirme | (enum + not) |
| `CategoriesController.Index/Create/Edit/Save/ChangeStatus/Delete` | `/admin/categories...` | Kategori CRUD (`GetFormAsync`, `SaveAsync` ...) | `ResultCategoryDto`, `UpdateCategoryDto` |
| `ProductsController.Index/Create/Edit/Save/ChangeStatus/Delete` | `/admin/products...` | Ürün CRUD (`GetAdminListAsync`, `GetFormAsync`, `SaveAsync` ...) | `ResultProductDto`, `UpdateProductDto` |
| `TablesController.Index/Save/Regenerate/ChangeStatus/Delete` | `/admin/tables...` | Masa ve QR yönetimi (`GetFormAsync`, `SaveAsync` ...) | `ResultTableDto`, `UpdateTableDto` |
| `CustomersController.Index` | GET `/admin/customers` | Müşteri listesi | `List<ResultCustomerDto>` |
| `NotificationsController.Index/MarkRead/ReadAll` | `/admin/notifications...` | Bildirim listesi (`GetPageAsync`) ve okundu işaretleme | `ResultNotificationPageDto` |

### View Component'ler
| Sınıf | Ne yapar | DTO |
|---|---|---|
| `CartCountViewComponent` | Navbar'daki sepet adedi | `int` |
| `CartDrawerViewComponent` | Sağdan açılan sepet paneli | `ResultOrderDto` |
| `AdminSidebarViewComponent` | Admin sol menüsü ve sayıları | DTO listelerinin sayısı |
| `AdminNotificationsViewComponent` | Üst çubuktaki bildirim zili | `List<ResultNotificationDto>` |
