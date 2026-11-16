# 04 · Servisler (sipariş dışındaki iş kuralları)

> Servisler iş kurallarının yaşadığı yerdir. Controller "ne istendiğini" bilir, repository "veriyi nasıl okuyup yazacağını", servis ise "hangi kurallarla" yapılacağını. Bu bölümde Category, Product, Table, Customer ve Notification servislerini satır satır inceliyoruz. OrderService ve desenler Bölüm 05'te.

---

## 0. Bütün servislerin ortak kalıbı

Neredeyse her servis aynı iskeleti izliyor. Bunu bir kez öğrenince hepsini okuyabilirsin:

```csharp
public class XService : IXService
{
    private readonly IXRepository _xRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    public XService(IXRepository xRepository, IUnitOfWork unitOfWork, IMapper mapper) { ... }   // constructor injection

    public async Task<List<ResultXDto>> GetAllAsync()        // OKUMA: entity → DTO
    public async Task CreateAsync(CreateXDto dto)             // EKLEME: DTO → entity → kontrol → kaydet
    {
        X x = _mapper.Map<X>(dto);
        await CheckAsync(x);
        await _xRepository.AddAsync(x);          // işaretle
        await _unitOfWork.SaveChangesAsync();    // yaz
    }
    public async Task UpdateAsync(UpdateXDto dto)             // GÜNCELLEME: kaydı bul → üstüne yaz → kontrol → kaydet
    {
        var x = await GetEntityAsync(dto.Id);
        _mapper.Map(dto, x);
        await CheckAsync(x);
        await _unitOfWork.SaveChangesAsync();    // x zaten izleniyor; Update çağırmaya gerek yok
    }
    public async Task ChangeStatusAsync(int id, bool isActive) // aktif/pasif
    public async Task DeleteAsync(int id)                      // SİLME: önce "silinebilir mi?" kontrolü

    private async Task<X> GetEntityAsync(int id)               // bulamazsa BusinessException("X not found.")
    private async Task CheckAsync(X x)                         // doğrulama + temizleme (Trim, boş → null)
}
```

İki kural her serviste aynı:
- **Sorgu repository'de yazılı.** Servis `_xRepository.GetAllWithYAsync()` gibi isimli bir metot çağırıyor; `Include`, `Where`, `ToListAsync` servis kodunda yok. BusinessLayer EF Core'u hiç görmüyor (Bölüm 02).
- **Kaydetme kararı serviste.** Repository'nin `AddAsync`/`Remove` metotları sadece işaretliyor; servis işi bitince `_unitOfWork.SaveChangesAsync()` çağırıyor. İzlenen (veritabanından okunmuş) bir kayıt değiştirildiyse sadece `SaveChangesAsync` yeterli.

### `await` atamalarında `var`
Projede `await` ile değer alınan her değişken `var` ile tanımlanıyor:
```csharp
var category = await GetEntityAsync(id);                       // tipi: Category
var products = await _productRepository.GetMenuAsync(category, search);  // tipi: List<Product>
var exists = await _tableRepository.AnyAsync(t => t.QrToken == token);    // tipi: bool
```
`var` "tipi yazma, derleyici sağ taraftan çıkarsın" demektir. Değişken yine **tam olarak o tipte** olur (C# dinamik değil, statik tiplidir); sadece aynı bilgiyi iki kez yazmıyoruz. Metodun ne döndürdüğü, metodun adından ve arayüzünden zaten belli. Visual Studio'da `var`'ın üstüne fare ile gelince gerçek tip görünür.

`new` ile oluşturulan nesnelerde ise tip açıkça yazılmaya devam ediyor (`Order order = new Order();`); orada okuyan kişi tipi ilk bakışta görür.

### `BusinessException`
```csharp
public class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }
}
```
Neden normal `Exception` değil de özel bir sınıf? Controller'lar **sadece** `catch (BusinessException ex)` yakalıyor:
- `BusinessException` = kullanıcının düzeltebileceği bir durum ("Category name must be 2-100 characters."). Mesaj ekranda gösterilir.
- Başka herhangi bir exception (SQL bağlantısı koptu, null reference...) = programcı veya sistem hatası. Yakalanmaz ve hata sayfasına düşer. Kullanıcıya iç ayrıntı (stack trace, SQL) sızdırılmaz.

### Neden servis bir interface'in arkasında? (`ICategoryService`)
1. DI ile değiştirilebilirlik (testte sahte servis).
2. Controller sadece sözleşmeyi görür.
3. Interface dosyası servisin "ne yapabildiğinin" kısa bir özetidir. Bir servisi okumaya interface'inden başla.

---

## 1. CategoryService

### GetAllAsync / GetActiveAsync / CountAsync
```csharp
// CategoryService: sadece repository'ye sorar ve DTO'ya çevirir
var categories = await _categoryRepository.GetAllWithProductsAsync();           // admin: hepsi
var categories = await _categoryRepository.GetActiveWithActiveProductsAsync();  // müşteri: aktifler

// CategoryRepository: sorgunun kendisi
return await Set
    .Where(c => c.IsActive)
    .Include(c => c.Products.Where(p => p.IsActive))   // filtered include
    .OrderBy(c => c.DisplayOrder)
    .ToListAsync();

// Admin menüsündeki rozet için sadece sayı
public async Task<int> CountAsync() => await _categoryRepository.CountAsync();   // SELECT COUNT(*)
```
- `Include`, EF Core'a "ilişkili tabloyu da getir" der (SQL'de JOIN). Yazılmazsa `category.Products` boş liste gelir. **EF Core lazy loading açık değil**, yani ilişkiler kendiliğinden yüklenmez.
- Burada ürünler sadece **saymak** için yükleniyor. Çok sayıda ürün olsaydı `Select(c => new { c, Count = c.Products.Count() })` gibi bir projeksiyonla SQL'de `COUNT` yaptırmak daha verimli olurdu.

### CreateAsync
```csharp
Category category = _mapper.Map<Category>(dto);
await CheckAsync(category);

if (category.DisplayOrder == 0)
{
    var count = await _categoryRepository.CountAsync();
    category.DisplayOrder = count + 1;      // sıra girilmediyse en sona
}
category.Slug = await MakeSlugAsync(category.Name, 0);
category.Icon = "bi-tag";                   // Bootstrap Icons'tan varsayılan ikon
category.CreatedAt = _time.GetLocalNow().Date;             // TimeProvider (Bölüm 01)

await _categoryRepository.AddAsync(category);
await _unitOfWork.SaveChangesAsync();                      // 1. kayıt → Id oluştu
category.Code = "CAT-" + category.Id.ToString("000");      // CAT-009
await _unitOfWork.SaveChangesAsync();                      // 2. kayıt (sadece Code kolonu UPDATE)
```
**İki kez kaydetme** nedeni: `Code` Id'den türetiliyor (`CAT-009`) ve Id ancak INSERT'ten sonra belli oluyor. İlk `SaveChangesAsync` dönünce EF Core `category.Id`'yi veritabanının ürettiği değerle doldurmuş oluyor. İkinci kayıtta `Update` çağrılmıyor: kategori zaten izleniyor, EF Core değişen `Code` alanını kendisi buluyor. Bugünkü testte "Test Kategori" Id=9 almıştı; Code'u `CAT-009` oldu.

İnce nokta: `Code` kolonu `IsRequired`, ama ilk INSERT'te `Code = ""` (boş metin) gidiyor. Boş metin NULL değil, bu yüzden kabul ediliyor.

`ToString("000")`: sayıyı en az 3 haneye tamamlar (9 → "009", 123 → "123").

### UpdateAsync
```csharp
var category = await GetEntityAsync(dto.Id);
int oldDisplayOrder = category.DisplayOrder;
_mapper.Map(dto, category);                      // Code, Slug, CreatedAt DTO'da yok → değişmez
if (dto.DisplayOrder == 0) category.DisplayOrder = oldDisplayOrder;
await CheckAsync(category);
category.Slug = await MakeSlugAsync(category.Name, category.Id);   // isim değiştiyse slug da değişir
await _unitOfWork.SaveChangesAsync();
```
Not: Slug değişirse eski adres (`/products?category=eski-ad`) artık çalışmaz. Gerçek bir sitede SEO için eski slug'dan yeniye yönlendirme tutulur.

### DeleteAsync
```csharp
var category = await GetEntityAsync(id);
var hasProducts = await _productRepository.AnyAsync(p => p.CategoryId == id);
if (hasProducts)
    throw new BusinessException(category.Name + " kategorisinde hâlâ ürün var. Önce ürünleri taşıyın veya silin.");
_categoryRepository.Remove(category);
await _unitOfWork.SaveChangesAsync();
```
Bugün test ettik: "Sıcak Kahveler kategorisinde hâlâ ürün var. Önce ürünleri taşıyın veya silin." `AnyAsync` → SQL `EXISTS`; bütün ürünleri çekmez, ilk eşleşmede durur.

### CheckAsync: doğrulama + normalleştirme
```csharp
category.Name = (category.Name ?? "").Trim();
if (string.IsNullOrWhiteSpace(category.Description)) category.Description = null;
if (string.IsNullOrWhiteSpace(category.ImageUrl)) category.ImageUrl = null;
if (category.Name.Length < 2 || category.Name.Length > 100) throw ...;
if (category.Description != null && category.Description.Length > 500) throw ...;
if (category.DisplayOrder < 0 || category.DisplayOrder > 999) throw ...;
var nameUsed = await _categoryRepository.AnyAsync(c => c.Name == category.Name && c.Id != category.Id);
```
- **Normalleştirme:** `"  Kahve  "` → `"Kahve"`; boş açıklama `""` → `null`. Veritabanında "boş" kavramının tek bir anlamı olsun.
- **`c.Id != category.Id`**: Güncellemede kategorinin kendi adı "zaten var" sayılmasın diye. Yeni kayıtta Id=0 olduğu için bütün kayıtlarla karşılaştırılır.
- Küçük tutarsızlık: mesaj "between 1 and 999" diyor ama kontrol `< 0`, yani 0'ı da kabul ediyor (0 "sıra girilmedi" anlamında kullanılıyor).
- Neden `[Required]`, `[StringLength]` gibi DTO attribute'ları değil de servis içinde kontrol? Kurallar BusinessLayer'da tek yerde kalıyor ve hangi arayüzden gelinirse gelinsin çalışıyor. Bedeli: istemci tarafı (JavaScript) doğrulaması yok, hata ancak gönderdikten sonra görünüyor.

### MakeSlugAsync
```csharp
string text = name.ToLower()
    .Replace("ç", "c").Replace("ğ", "g").Replace("ı", "i")
    .Replace("ö", "o").Replace("ş", "s").Replace("ü", "u");
string slug = Regex.Replace(text, "[^a-z0-9]+", "-").Trim('-');
if (slug == "") slug = "category";

string result = slug;
int number = 2;
while (await _categoryRepository.AnyAsync(c => c.Slug == result && c.Id != id))
{
    result = slug + "-" + number;
    number++;
}
return result;
```
Adım adım `"Tea & İnfüzyonlar"` için:
1. `ToLower()` → `"tea & i̇nfüzyonlar"` (ToLower sunucunun diline duyarlı: Türkçe bir sunucuda `İ` → `i`, İngilizce bir sunucuda `i̇` (i + birleşik nokta) olur; o noktayı da regex bir sonraki adımda tireye çevirir. Küçük bir kenar durumu; `ToLowerInvariant()` yerine `ToLower(new CultureInfo("tr-TR"))` kullanmak sonucu sabitlerdi.)
2. Türkçe harf değişimi → `"tea & infuzyonlar"`
3. Regex `[^a-z0-9]+`: harf ve rakam **olmayan** ardışık her grubu tek bir `-` yap → `"tea-infuzyonlar"`
4. `Trim('-')`: baştaki ve sondaki tireleri sil.
5. Döngü: `tea-infuzyonlar` varsa `-2`, o da varsa `-3`...

`HasIndex(x => x.Slug).IsUnique()` ikinci koruma: iki admin aynı anda aynı adla kayıt yaparsa (race condition) veritabanı birini reddeder.

---

## 2. ProductService

### Statik rozet listesi
```csharp
public static readonly IReadOnlyList<string> Badges = new[] { "Çok Satan", "Yeni", "Popüler", "Şefin Seçimi" };
```
Hem admin formundaki açılır liste hem de doğrulama bu listeyi kullanıyor. Tek kaynak (single source of truth).

Neden `IReadOnlyList`? `public static` bir alan uygulamanın her yerinden erişilebilir. Eskiden `List<string>` idi; herhangi bir kod `ProductService.Badges.Add("x")` yazıp **bütün kullanıcılar için** listeyi değiştirebilirdi. `readonly` sadece alanın başka bir listeye atanmasını engeller, listenin içini değil. `IReadOnlyList` ise `Add`/`Remove` sunmuyor.

### `Active()`: tekrar kullanılan sorgu parçası (ProductRepository'de)
```csharp
private IQueryable<Product> Active()
{
    return Set
        .Include(p => p.Category)
        .Where(p => p.IsActive && p.Category.IsActive);
}
```
Henüz çalışmamış bir sorgu (`IQueryable`). `ProductRepository`'nin müşteri tarafı metotları (`GetMenuAsync`, `GetFeaturedAsync`, `GetActiveByIdAsync`, `GetRelatedAsync`) bunun üstüne ekleme yapıyor. `private` olduğu için repository'nin dışına çıkmıyor; servis sadece sonucu (`List<Product>`) görüyor. "Menüde görünür" kuralı (ürün aktif **ve** kategorisi aktif) tek bir yerde yazılı. Bu fikir case'teki **Specification** desenine çok yakın: tekrar kullanılabilir bir filtre kuralı. Tam bir Specification için kural bir sınıf olurdu (`ActiveProductSpec : ISpecification<Product>`).

### GetMenuAsync: arama ve filtre
```csharp
// ProductService
var products = await _productRepository.GetMenuAsync(category, search);

// ProductRepository.GetMenuAsync
IQueryable<Product> query = Active();
if (!string.IsNullOrEmpty(categorySlug))
    query = query.Where(p => p.Category.Slug == categorySlug);
if (!string.IsNullOrWhiteSpace(search))
    query = query.Where(p => p.Name.Contains(search) || p.Category.Name.Contains(search));
return await query.OrderBy(p => p.Category.DisplayOrder).ThenBy(p => p.Id).ToListAsync();
```
- **Koşullu sorgu inşası:** Her `if` sorguya bir `WHERE` daha ekliyor. `ToListAsync()` denene kadar SQL'e gidilmiyor. Sonuçta tek bir SQL üretiliyor.
- `Contains(search)` → SQL `LIKE '%latte%'`. Büyük/küçük harf duyarlılığını **veritabanının collation'ı** belirler. SQL Server'ın varsayılanı (`..._CI_AS`) duyarsız olduğu için "LATTE" de bulunur.
- `search` parametre olarak gönderilir (`@__search_0`), SQL injection riski yok.
- Test: `/products?q=latte` → Latte'ler; `?category=pastalar` → pastalar.

### GetFeaturedAsync / GetRelatedAsync
```csharp
Active().Where(p => p.Badge != null).OrderBy(...).Take(count)                  // ana sayfa: rozetliler
Active().Where(p => p.CategoryId == categoryId && p.Id != productId).Take(count) // "Yanında iyi gider"
```
`Take(count)` → SQL `TOP(@count)`.

### GetActiveByIdAsync
Pasif bir ürünün detay sayfası açılmasın diye `FindAsync` değil `_productRepository.GetActiveByIdAsync(id)` (içinde `Active().FirstOrDefaultAsync(...)`) kullanılıyor. Bulunamazsa `null` döner ve controller 404 gösterir (testte `/products/9999` → 404).

### GetAdminListAsync: filtre SQL'de
```csharp
var products = await _productRepository.GetAdminListAsync(categoryId, search);
```
Eskiden bütün ürünler belleğe çekilip kategori ve ad filtresi C#'ta (`StringComparison.CurrentCultureIgnoreCase`) uygulanıyordu. Artık filtre repository'de `Where` olarak yazılı ve SQL'de çalışıyor. Büyük/küçük harf duyarsızlığı SQL Server'ın collation'ından geliyor (yukarıdaki `GetMenuAsync` gibi). Küçük fark: varsayılan collation Türkçe "ı/I" eşleşmesini bilmiyor.

`ProductService.CountAsync()` ise admin menüsündeki rozet için tek bir `SELECT COUNT(*)`.

### DeleteAsync
```csharp
var usedInOrders = await _orderRepository.AnyContainingProductAsync(id);
if (usedInOrders) throw new BusinessException(product.Name + " geçmiş siparişlerde var, silinemez. Bunun yerine pasif yapın.");
_productRepository.Remove(product);
await _unitOfWork.SaveChangesAsync();
```
`AnyContainingProductAsync` SQL'de `EXISTS (SELECT ... FROM OrderDetails WHERE ProductId = @id)` olur. ProductService, OrderRepository'yi de istiyor; servis birden fazla repository kullanabilir.

### CheckAsync
```csharp
if (product.BasePrice <= 0 || product.BasePrice > 10000) throw ...;
if (product.Badge != null && !Badges.Contains(product.Badge)) throw ...;
var categoryExists = await _categoryRepository.AnyAsync(c => c.Id == product.CategoryId);
```
`categoryExists` kontrolü önemli: form manipüle edilip `CategoryId=999` gönderilirse kibar bir hata verir. Kontrol olmasaydı FK ihlali yüzünden SQL exception oluşur ve hata sayfası çıkardı.

`CleanText` yardımcı metodu: `static`, çünkü hiçbir alan kullanmıyor. Boşsa `null` döner, doluysa `Trim()` yapar.

---

## 3. TableService

### GetByQrAsync: QR doğrulama
```csharp
if (string.IsNullOrWhiteSpace(qrToken)) return null;
string token = qrToken.Trim().ToUpper();
var table = await _tableRepository.GetFirstAsync(t => t.QrToken == token && t.IsActive);
```
- `ToUpper()`: QR'ı elle yazan biri küçük harf kullanabilir. Bugün `/cart?qr=cf-t12-peach` denedik ve çalıştı.
- `t.IsActive`: pasif masanın kodu çalışmaz.
- Case maddesi: *"QR bağlantıları masa kaydıyla doğrulanmalı."*

### NewQrAsync: güvenli rastgele kod
```csharp
const string letters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";   // 0, O, 1, I yok
while (true)
{
    string random = "";
    for (int i = 0; i < 6; i++)
        random += letters[Random.Shared.Next(letters.Length)];
    string token = "CF-T" + number.ToString("00") + "-" + random;    // CF-T07-K4P9XZ
    if (!await _tableRepository.AnyAsync(t => t.QrToken == token)) return token;
}
```
- Karışan karakterler (0/O, 1/I) çıkarılmış; kart üzerinde okunabilir olsun diye.
- 32 karakterden 6 tanesi → 32⁶ ≈ 1 milyar olasılık. Tahmin edilmesi zor; böylece biri masa numarasından QR kodunu türetip evinden "masa siparişi" veremez.
- `Random.Shared`: thread-safe, paylaşılan rastgele sayı üreticisi (.NET 6+). Kriptografik değil. Gerçek güvenlik gerekseydi `RandomNumberGenerator` kullanılırdı.
- `while (true)` + benzersizlik kontrolü: çakışma olursa tekrar dener (pratikte neredeyse hiç olmaz).
- Seed'deki `CF-T07-GUL` bu formata uymayan, elle yazılmış bir demo kodu.

### UpdateAsync: numara değişirse QR değişir
```csharp
int oldNumber = table.Number;
_mapper.Map(dto, table);                  // QrToken DTO'da yok → korunur
await CheckAsync(table);
if (table.Number != oldNumber)
    table.QrToken = await NewQrAsync(table.Number);
```
Kural: masa 7'nin kartı masa 8'e taşındıysa eski kart çalışmamalı.

### GetFormAsync: yeni masa numarası
```csharp
form.Number = await _tableRepository.GetMaxNumberAsync() + 1;   // hiç masa yoksa 0 + 1 = 1
```
`GetMaxNumberAsync` içinde `MaxAsync(t => (int?)t.Number) ?? 0` var. `(int?)` dönüşümü önemli: boş bir tabloda `MAX` NULL döner; `int` beklenseydi EF Core exception fırlatırdı. Eskiden bu yüzden önce `AnyAsync` ile "masa var mı?" diye ayrı bir sorgu atılıyordu.

### DeleteAsync
`_tableRepository.GetWithOrdersAsync(id)` ile siparişleri yükleyip `Count > 0` kontrolü yapıyor. (`AnyAsync` ile daha verimli yazılabilirdi; sonuç aynı.)

---

## 4. CustomerService
```csharp
var customers = await _customerRepository.SearchWithOrdersAsync(search);   // Include + arama + sıralama repository'de
return _mapper.Map<List<ResultCustomerDto>>(customers);
```
Salt okunur. Müşteri oluşturma burada değil, `OrderService.PlaceOrderAsync` içinde. Toplam harcama ve son sipariş tarihi AutoMapper eşlemesinde (`CustomerMappings`) hesaplanıyor (Bölüm 03).

---

## 5. NotificationService: okuyan taraf
Sınıfın yorumu önemli: *"Bildirimleri YAZAN taraf Events klasöründeki observer'lardır."* Okuma ile yazma ayrılmış. Bu, CQRS'in (Command Query Responsibility Segregation) çok basit bir yansıması.

```csharp
public async Task<List<ResultNotificationDto>> GetAllAsync(NotificationTarget? target, bool unreadOnly)
{
    var notifications = await _notificationRepository.GetFilteredAsync(target, unreadOnly);
    return _mapper.Map<List<ResultNotificationDto>>(notifications);
}
```
`GetFilteredAsync` repository'de koşullu sorgu kuruyor (`target` varsa `Where`, `unreadOnly` ise `Where`) ve `Include(n => n.Order)` yapıyor (mapping'de `OrderNo` için).

```csharp
public async Task MarkAllReadAsync(NotificationTarget? target)
{
    List<Notification> unread = ...GetWhereAsync(n => !n.IsRead [&& n.Target == target]);
    foreach (Notification notification in unread)
    {
        notification.IsRead = true;               // nesneler izleniyor: EF değişikliği görür
    }
    await _unitOfWork.SaveChangesAsync();         // hepsi tek SaveChanges
}
```
**Eskiden** döngünün içinde her bildirim için `UpdateAsync` → ayrı bir `SaveChanges` çağrılıyordu: 18 okunmamış bildirim = 18 veritabanı turu (N+1 yazma). Unit of Work ile döngü sadece bellekteki değeri değiştiriyor, en sonda **tek** `SaveChanges` bütün UPDATE'leri bir arada gönderiyor.

Daha da ileri gitmek istersen: EF Core 7+ `ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true))` bildirimleri hiç belleğe çekmeden tek bir SQL `UPDATE ... WHERE` çalıştırır.

---

## Kendini sına
1. Kategori kodunu (`CAT-009`) üretmek için neden iki kez kaydediliyor?
2. `CheckAsync` içindeki `c.Id != category.Id` olmasaydı, adını değiştirmeden bir kategoriyi kaydetmeye çalışınca ne olurdu?
3. `ProductRepository.Active()` neden `List` değil de `IQueryable` döndürüyor? Neden `private`?
4. QR kodlarında neden 0, O, 1 ve I harfleri yok? Kod neden rastgele?
5. `MarkAllReadAsync` eskiden 100 bildirim için kaç `SaveChanges` turu yapıyordu? Şimdi kaç? `ExecuteUpdateAsync` ile ne değişirdi?
6. `CategoryService.UpdateAsync` neden `_categoryRepository.Update(category)` çağırmıyor?
