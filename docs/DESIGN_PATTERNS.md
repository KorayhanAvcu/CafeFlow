# CafeFlow: Tasarım Desenleri

Case'te listelenen 10 desenden **6 tanesi** uygulanmıştır: Strategy, Factory Method, Observer, State, Decorator ve Chain of Responsibility. Hepsi `CafeOrder.BusinessLayer` içindedir. Sipariş verme ve durum değiştirme akışlarında `OrderService` (`Services/Orders/OrderService.cs`) tarafından çalıştırılır.

```text
Sipariş verilirken (POST /cart/checkout → OrderService.PlaceOrderAsync)
  Factory Method → Decorator → Strategy → Chain of Responsibility → kayıt (transaction) → Observer

Durum değişirken (POST /admin/orders/{id}/status → OrderService.ChangeStatusAsync)
  State → durum geçmişi + kayıt → Observer
```

---

## 1. Decorator: seçeneklere göre ürün fiyatı

**Amaç:** Kahveye boyut, süt, ekstra shot ve şurup gibi seçenekler eklendiğinde birim fiyatı, her seçeneği taban fiyatın üstüne bir "kat" olarak sararak hesaplamak. Yeni bir seçenek türü eklendiğinde hesap kodu değişmez.

| Sınıf | Görevi |
|---|---|
| `Pricing/IPriceComponent` | Ortak arayüz: `decimal GetPrice()` |
| `Pricing/BaseProductPrice` | En içteki katman: ürünün seçeneksiz fiyatı (`Product.BasePrice`) |
| `Pricing/OptionPriceDecorator` | İçteki fiyatı sarar ve seçeneğin farkını (`ProductOption.PriceDelta`) ekler |

**Tetiklendiği akış:** Sepet sayfası, sepet paneli ve sipariş kaydı. Hepsi `OrderService.CalculateOrderAsync` → `CalculateUnitPrice` üzerinden çalışır. Fiyatlar her seferinde veritabanından okunur; tarayıcıdan gelen fiyat kullanılmaz.

```text
Vanilyalı Latte 160 → Büyük +30 → Yulaf Sütü +15 → Ekstra shot +20 = 225 ₺
new OptionPriceDecorator(new OptionPriceDecorator(new OptionPriceDecorator(new BaseProductPrice(160), 30), 15), 20)
```

## 2. Strategy: kanala göre teslimat ücreti

**Amaç:** Teslimat, gel-al ve masa siparişleri farklı ücret kuralı kullanır. Kurallar ayrı sınıflardadır; yeni bir kanal geldiğinde mevcut kurallar değişmez.

| Sınıf | Kural |
|---|---|
| `Pricing/Fees/IDeliveryFeeStrategy` | `decimal Calculate(decimal orderAmount)` |
| `Pricing/Fees/DeliveryFeeStrategy` | 30 ₺; ara toplam 600 ₺ ve üzerindeyse ücretsiz (değerler `appsettings.json` → `Pricing`, `PricingOptions`) |
| `Pricing/Fees/PickupFeeStrategy` | 0 ₺ |
| `Pricing/Fees/TableFeeStrategy` | 0 ₺ |

**Tetiklendiği akış:** `OrderService.CalculateOrderAsync` içinde ara toplam hesaplandıktan sonra `GetFeeStrategy(order.Channel)` doğru stratejiyi seçer ve `DeliveryFee` ile `Total` hesaplanır. Müşteri sepette sipariş türünü değiştirdiğinde özet bu stratejiyle yeniden hesaplanır.

## 3. Factory Method: kanala göre sipariş nesnesi

**Amaç:** Her sipariş kanalı farklı alanlar ister: teslimat için müşteri ve adres, gel-al için müşteri ve alma saati, masa için QR ile doğrulanmış masa. Ortak adımlar (sipariş numarası, `Received` durumu, tarih, ilk durum kaydı) tek yerde yazılır; kanala özgü kısmı alt sınıf doldurur.

| Sınıf | Görevi |
|---|---|
| `Creation/OrderCreator` (abstract) | `Create(form, table)`: ortak kısım + `CreateForChannel` (factory method) çağrısı |
| `Creation/DeliveryOrderCreator` | Müşteri, adres ve adres notu |
| `Creation/PickupOrderCreator` | Müşteri ve alma saati |
| `Creation/TableOrderCreator` | QR koduyla bulunan masa (`TableId`) |

**Tetiklendiği akış:** `OrderService.PlaceOrderAsync` → `GetCreator(form.Channel).Create(form, table)`. Kanal Session'dan gelir; masa siparişinde masa, veritabanındaki **aktif** bir masanın QR koduyla bulunur.

## 4. Chain of Responsibility: sipariş doğrulamaları

**Amaç:** Sipariş kaydedilmeden önce yapılan kontrolleri birbirinden bağımsız halkalara ayırmak. Her halka yalnızca kendi kuralına bakar. Sorun bulursa `BusinessException` fırlatır ve zincir durur; yoksa siparişi sıradaki halkaya geçirir. Yeni bir kural, yeni bir halka eklenerek yazılır.

| Halka | Kontrol |
|---|---|
| `Validation/OrderValidationHandler` (abstract) | `SetNext`, `Handle`, `Check` |
| 1. `ProductHandler` | Sepet boş mu, ürün ve kategorisi aktif mi, özelleştirilemeyen ürüne seçenek var mı, aynı gruptan iki seçenek var mı |
| 2. `QuantityHandler` | Her satırda adet 1 ile 20 arasında mı |
| 3. `ChannelInfoHandler` | Masa: geçerli QR. Teslimat ve gel-al: ad ve telefon. Teslimat: adres. Gel-al: ileri bir saat |
| 4. `OrderAmountHandler` | Teslimatta ara toplam en az 100 ₺ mi |

**Tetiklendiği akış:** `OrderService.PlaceOrderAsync` içinde fiyat hesabından sonra, kayıttan önce. Hata olursa sipariş kaydedilmez; `CartController.Checkout` sepet sayfasını, girilen bilgiler kaybolmadan hata mesajıyla tekrar gösterir.

## 5. State: sipariş durum geçişleri

**Amaç:** Hangi durumdan hangi duruma geçilebileceğini her durumun kendi sınıfına bırakmak. Böylece iş kuralları `if/else` yığınına dönüşmez.

```text
Received (Alındı) → Preparing (Hazırlanıyor) → Ready (Hazır) → Delivered (Teslim Edildi)
   ↓           ↓
Cancelled   Cancelled            (Delivered ve Cancelled son durumdur)
```

| Sınıf | İzinli sonraki durumlar |
|---|---|
| `States/OrderState` (abstract) | `GetAllowedNext()`, `CanMoveTo(target)`, `For(status)` |
| `ReceivedState` | Preparing, Cancelled |
| `PreparingState` | Ready, Cancelled |
| `ReadyState` | Delivered |
| `DeliveredState`, `CancelledState` | (yok) |

**Tetiklendiği akış:** Admin sipariş detayındaki durum düğmeleri → `OrdersController.ChangeStatus` → `OrderService.ChangeStatusAsync`. İzinsiz geçiş `BusinessException` ile reddedilir. İzinli geçişte `OrderStatusHistories` tablosuna "önceki → yeni" kaydı (isteğe bağlı notla) eklenir. Detay sayfası düğmeleri de `GetAllowedNext()` sonucuna göre gösterir.

## 6. Observer: mutfak, kasa ve admin bildirimleri

**Amaç:** Sipariş verildiğinde veya durumu değiştiğinde ilgili bileşenleri, `OrderService` onları tek tek tanımadan haberdar etmek. Yeni bir dinleyici (örneğin SMS) yalnızca `Program.cs`'e kayıt eklenerek bağlanır.

| Sınıf | Görevi |
|---|---|
| `Events/IOrderObserver` | `OrderPlacedAsync`, `StatusChangedAsync` |
| `Events/OrderEventPublisher` | DI'dan gelen bütün dinleyicilere olayı iletir; bir dinleyici hata verirse loglar, diğerleri yine haber alır |
| `Events/KitchenObserver` | Yeni siparişte hazırlanacak ürün sayısı; iptalde "hazırlamayı durdur" |
| `Events/CashierObserver` | Yeni siparişte toplam tutar; teslimde "teslim edildi" |
| `Events/AdminObserver` | Her yeni sipariş ve her durum değişikliği. Panelin zil simgesinde ve Bildirimler sayfasında görünür |

**Tetiklendiği akış:** `PlaceOrderAsync` sonunda `OrderPlacedAsync`, `ChangeStatusAsync` sonunda `StatusChangedAsync`. Bildirimler `Notifications` tablosuna yazılır (gerçek SMS/e-posta yerine simülasyon).

---

## Test edilen senaryolar

Aşağıdaki senaryolar çalışan uygulama ve SQL Server üzerinde denenmiştir.

| Senaryo | Beklenen | Desen |
|---|---|---|
| Vanilyalı Latte Büyük + Yulaf Sütü + Shot | Birim fiyat 225 ₺ | Decorator |
| Aynı ürün, farklı seçenekler | Ayrı sipariş kalemleri | Decorator |
| Teslimat, ara toplam 275 ₺ | Ücret 30 ₺, toplam 305 ₺ | Strategy |
| Teslimat, ara toplam 715 ₺ | Ücret 0 ₺ | Strategy |
| Gel-al / masa | Ücret 0 ₺ | Strategy |
| `/cart?qr=CF-T07-GUL` ile sipariş | Sipariş Masa 7'ye bağlanır | Factory Method |
| Teslimat, ara toplam 95 ₺ | "Teslimat için en az sipariş tutarı 100 ₺." | Chain |
| Adressiz teslimat | "Lütfen teslimat adresinizi girin." | Chain |
| Geçmiş alma saati | "Teslim alma saati geçmişte olamaz." | Chain |
| Geçersiz QR ile masa siparişi | "Lütfen masanızdaki QR kodu okutun." | Chain |
| Boş sepet | "Sepetiniz boş." | Chain |
| Alındı → Hazırlanıyor → Hazır → Teslim Edildi | Hepsi kabul edilir, 4 geçmiş kaydı oluşur | State |
| Alındı → Hazır, Hazır → İptal, Teslim Edildi → İptal, İptal → Hazırlanıyor | Reddedilir ("Sipariş ... durumundan ... durumuna geçemez.") | State |
| Yeni sipariş | Kitchen, Cashier ve Admin bildirimi | Observer |
| Durum değişikliği / iptal / teslim | Admin bildirimi; iptalde Kitchen, teslimde Cashier bildirimi | Observer |
