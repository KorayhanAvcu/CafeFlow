# CafeFlow kod rehberi: buradan başla

Bu klasör, CafeFlow projesindeki **bütün kodu** öğrenmen için yazıldı. Her bölüm gerçek koddan alıntılar içeriyor ve "ne yapıyor" sorusunun yanında "neden böyle yazılmış", "hangi tuzağı önlüyor" ve "nerede daha iyi olabilirdi" sorularını da cevaplıyor. Örnekler, bugün SQL Server üzerinde gerçekten çalıştırıp test ettiğimiz siparişlerden geliyor.

## Okuma sırası

| # | Bölüm | Ne öğreneceksin | Süre |
|---|---|---|---|
| 01 | [Katmanlar ve Program.cs](01-katmanlar-ve-program.md) | Projeler birbirini nasıl tanıyor, ayarlar nasıl birleşiyor, DI ömürleri, middleware sırası, attribute routing | 30 dk |
| 02 | [Entity'ler ve veritabanı](02-entity-ve-veritabani.md) | Tablolar, Fluent API, Restrict, migration geçmişi, seed, generic repository, change tracker, transaction | 45 dk |
| 03 | [DTO ve Mapster](03-dto-ve-mapster.md) | Over-posting, flattening, `Map<T>` ile `Map(src, dest)` farkı, Include tuzakları | 25 dk |
| 04 | [Servisler](04-servisler.md) | CRUD kalıbı, BusinessException, slug üretimi, QR üretimi, IQueryable ile sorgu inşası | 40 dk |
| 05 | [**OrderService ve 6 desen**](05-siparis-servisi-ve-desenler.md) | Decorator, Strategy, Factory Method, Chain of Responsibility, State, Observer: satır satır, gerçek bir siparişle | 90 dk |
| 06 | [Müşteri arayüzü](06-musteri-arayuzu.md) | PRG, TempData, antiforgery, Session sepeti, CartController, View Component, Razor layout, JS'siz arayüz | 50 dk |
| 07 | [Admin paneli ve Identity](07-admin-paneli-ve-identity.md) | Area, `[Authorize]` kalıtımı, şifre hash'i, lockout, claim, güvenli yönlendirme, admin CRUD | 45 dk |

> Desenler için acelen varsa doğrudan **05**'ten başla, ama 01 ve 02'deki "aynı DbContext" ve "change tracker" konuları 05'teki bazı ayrıntıları anlamak için gerekli.

## Dosya → bölüm haritası

| Dosya / klasör | Bölüm |
|---|---|
| `CafeOrder.slnx`, `*.csproj`, `appsettings*.json`, `Program.cs` | 01 |
| `EntityLayer/Entities/*`, `EntityLayer/Enums/*` | 02 |
| `DataAccessLayer/Context/*` (DbContext, Configurations, SeedData) | 02 |
| `DataAccessLayer/Migrations/*` | 02 |
| `DataAccessLayer/Repositories/*` | 02 |
| `DtoLayer/Dtos/*` | 03 |
| `BusinessLayer/Mapping/GeneralMapping.cs` | 03 |
| `BusinessLayer/Exceptions/BusinessException.cs` | 04 |
| `BusinessLayer/Services/{Categories,Products,Tables,Customers,Notifications}` | 04 |
| `BusinessLayer/Services/Orders/*` | 05 |
| `BusinessLayer/Pricing/*` (Decorator), `Pricing/Fees/*` (Strategy) | 05 |
| `BusinessLayer/Creation/*` (Factory Method) | 05 |
| `BusinessLayer/Validation/*` (Chain of Responsibility) | 05 |
| `BusinessLayer/States/*` (State) | 05 |
| `BusinessLayer/Events/*` (Observer) | 05 |
| `WebUI/Controllers/*`, `WebUI/Cart/*`, `WebUI/ViewComponents/*`, `WebUI/Models/*` | 06 |
| `WebUI/Views/*`, `wwwroot/js/*` | 06 |
| `EntityLayer/Enums/EnumTexts.cs`, `WebUI/ModelBinding/*` (Türkçe metinler ve sayı okuma) | 01 |
| `WebUI/Identity/*` | 07 |
| `WebUI/Areas/Admin/*` | 07 |

## Tek cümlelik özet

> Müşteri Session'da fiyatsız bir sepet tutar. Sipariş verdiğinde `OrderService`, **Factory Method** ile kanala uygun siparişi kurar, **Decorator** ile her kalemin fiyatını ve **Strategy** ile ücreti veritabanından yeniden hesaplar, **Chain of Responsibility** ile doğrular, tek transaction'da kaydeder ve **Observer** ile mutfak, kasa ve admine haber verir. Admin durum değiştirdiğinde **State** geçişin izinli olup olmadığına karar verir, geçmiş yazılır ve yine **Observer** çalışır.

## Projede fark etmen gereken zayıf noktalar (toplu liste)

Kod çalışıyor ve case'i karşılıyor. Aşağıdakiler "bir sonraki seviye" için. Sözlü sınavda veya mülakatta bunları kendiliğinden söyleyebilmek çok değerli:

| # | Konu | Nerede | Bölüm |
|---|---|---|---|
| 1 | Strategy ve Factory seçimi `if/else` ile yapılıyor (Open/Closed tam değil) | `OrderService.GetFeeStrategy`, `GetCreator` | 05 |
| 2 | State sadece geçiş tablosu; durum sınıflarında davranış yok | `States/OrderState.cs` | 05 |
| 3 | Bir observer'ın kaydı patlarsa başarısız entity tracker'da kalıp sonrakileri de bozabilir | `OrderEventPublisher` | 05 |
| 4 | Bildirimler sipariş transaction'ının dışında yazılıyor (Outbox yok) | `PlaceOrderAsync` | 05 |
| 5 | `UpdateAsync` → `_context.Update(order)` bütün grafiği Modified yapıyor | `ChangeStatusAsync` | 02, 05 |
| 6 | Her repository metodu kendi `SaveChanges`'ini çağırıyor (Unit of Work yok) | `GenericRepository` | 02 |
| 7 | Admin sipariş listesi ve dashboard bütün siparişleri RAM'e çekiyor | `OrdersController`, `DashboardController` | 07 |
| 8 | Sidebar sadece sayı göstermek için bütün kategori ve ürünleri yüklüyor | `AdminSidebarViewComponent` | 07 |
| 9 | "Hepsini okundu yap" her bildirim için ayrı UPDATE yapıyor | `NotificationService.MarkAllReadAsync` | 04 |
| 10 | Aynı telefonla gelen farklı isim yok sayılıyor | `PlaceOrderAsync` | 05 |
| 11 | `DateTime.Now` (UTC değil) | birçok yer | 05 |
| 12 | ~~AutoMapper 13.0.1 güvenlik uyarısı~~ → **çözüldü:** ücretsiz Mapster 10'a geçildi | `BusinessLayer.csproj` | 03 |
| 13 | Birim testi yok | — | — |

## Bugün bu projede ne yaptık?

1. Case maddelerini kodla karşılaştırdık. 6 desen çalışan akışlara bağlı; eksik olanlar teslim dosyalarıydı.
2. Uygulamayı `DESKTOP-NDMQ7CM\SQLEXPRESS`'e bağladık. Açılışta `MigrateAsync` 5 migration'ı uyguladı, `SeedData` menüyü ekledi.
3. HTTP üzerinden 7 sipariş verdik ve 5 kural ihlalini denedik (Decorator, Strategy, Factory, Chain, Observer).
4. Durum geçişlerini servis üzerinden test ettik: 10 izinli geçiş geçti, 6 izinsiz geçiş engellendi (State).
5. Admin şifresini düzelttik, admin sayfalarını ve eylemlerini test ettik.
6. Teslim dosyalarını ekledik: `database/` (şema ve örnek siparişler), `docs/screenshots/`, `docs/DESIGN_PATTERNS.md`, README.
7. Bu rehberi yazdık.

## Kendi kendine çalışma planı

1. **Oku:** Her bölümü ilgili dosya yanında açıkken oku.
2. **Kır:** Bir kuralı bilerek boz ve sonucu gör. Örneğin `SetNext` içinde `return this;` yaz, sonra adressiz bir teslimat siparişi ver. Ne oldu? (Bölüm 05, Chain)
3. **İzle:** Visual Studio'da `OrderService.PlaceOrderAsync` başına breakpoint koy, sepetten sipariş ver ve F10/F11 ile her deseni adım adım izle. `price` değişkeninin içini (Decorator katmanları) Watch penceresinde aç.
4. **Genişlet** (her biri tek bir desene dokunur):
   - Strategy: hafta sonu teslimat 40 ₺.
   - Chain: gece 23:00–07:00 arası teslimat yok.
   - Observer: konsola log yazan `SmsObserver`.
   - State: Ready → Cancelled izni (1 satır).
   - Decorator: yüzdelik bir seçenek (`PercentPriceDecorator`).
5. **Yeni desen ekle:** Command (admin eylemleri) veya Specification (sipariş filtreleri). Bölüm 05'in sonunda nereye oturacakları anlatılıyor.
6. **Test yaz:** Bir xUnit projesi aç ve `OrderState`, `DeliveryFeeStrategy`, `OptionPriceDecorator` ve zincir halkaları için birim testleri yaz. Bu sınıflar veritabanına bağımlı olmadığı için test etmesi çok kolay. "Test edilebilir mimari"nin anlamı bu.
