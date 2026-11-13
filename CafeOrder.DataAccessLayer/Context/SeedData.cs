using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;
using Microsoft.EntityFrameworkCore;

namespace CafeOrder.DataAccessLayer.Context;

/// <summary>
/// Veritabanı boşsa başlangıç verilerini (kategori, ürün, seçenek, masa) bir kez ekler.
/// Program.cs uygulama açılırken çağırır. Kategori tablosunda kayıt varsa hiçbir şey yapmaz.
/// </summary>
public static class SeedData
{
    /// <summary>Veritabanı boşsa Türkçe başlangıç menüsünü (kategori, ürün, seçenek, masa) tek seferde ekler. Kategori varsa hiçbir şey yapmaz.</summary>
    public static async Task EnsureSeededAsync(CafeOrderDbContext context)
    {
        // Daha önce veri eklenmişse tekrar ekleme.
        var hasData = await context.Categories.AnyAsync();
        if (hasData)
        {
            return;
        }

        // 1) Kategoriler
        var hot = new Category { Code = "CAT-001", Slug = "sicak-kahveler", Name = "Sıcak Kahveler", Description = "Kadifemsi lattelar, yoğun espressolar ve içinizi ısıtan imza demlemeler.", Icon = "bi-cup-hot", ImageUrl = ImageUrl("1509042239860-f550ce710b93"), DisplayOrder = 1, IsActive = true, CreatedAt = new DateTime(2026, 1, 12) };
        context.Categories.Add(hot);
        var iced = new Category { Code = "CAT-002", Slug = "soguk-kahveler", Name = "Soğuk Kahveler", Description = "Buz üzerinde espresso içecekleri ve yavaş demlenmiş cold brew.", Icon = "bi-cup-straw", ImageUrl = ImageUrl("1461023058943-07fcbe16d735"), DisplayOrder = 2, IsActive = true, CreatedAt = new DateTime(2026, 1, 12) };
        context.Categories.Add(iced);
        var signature = new Category { Code = "CAT-003", Slug = "imza-icecekler", Name = "İmza İçecekler", Description = "Pembe favorilerimiz: çilekli, güllü ve orman meyveli tarifler.", Icon = "bi-stars", ImageUrl = ImageUrl("1572490122747-3968b75cc699"), DisplayOrder = 3, IsActive = true, CreatedAt = new DateTime(2026, 2, 3) };
        context.Categories.Add(signature);
        var cakes = new Category { Code = "CAT-004", Slug = "pastalar", Name = "Pastalar ve Tatlılar", Description = "Her sabah taze pişen pastalar, cheesecake'ler ve makaronlar.", Icon = "bi-cake2", ImageUrl = ImageUrl("1578985545062-69928b1d9587"), DisplayOrder = 4, IsActive = true, CreatedAt = new DateTime(2026, 2, 3) };
        context.Categories.Add(cakes);
        var bakery = new Category { Code = "CAT-005", Slug = "firin", Name = "Fırın", Description = "Tereyağlı kruvasanlar, tarçınlı rulolar ve sabah hamur işleri.", Icon = "bi-basket", ImageUrl = ImageUrl("1555507036-ab1f4038808a"), DisplayOrder = 5, IsActive = true, CreatedAt = new DateTime(2026, 3, 18) };
        context.Categories.Add(bakery);
        var snacks = new Category { Code = "CAT-006", Slug = "atistirmaliklar", Name = "Atıştırmalıklar", Description = "Günün her saatine tostlar, sandviçler ve hafif lezzetler.", Icon = "bi-egg-fried", ImageUrl = ImageUrl("1541519227354-08fa5d50c44d"), DisplayOrder = 6, IsActive = true, CreatedAt = new DateTime(2026, 4, 7) };
        context.Categories.Add(snacks);
        var tea = new Category { Code = "CAT-007", Slug = "caylar", Name = "Çaylar ve Bitki Çayları", Description = "Yaprak çaylar, bitki çayları ve matcha alternatifleri.", Icon = "bi-cup", ImageUrl = ImageUrl("1544787219-7f47ccb76574"), DisplayOrder = 7, IsActive = true, CreatedAt = new DateTime(2026, 8, 20) };
        context.Categories.Add(tea);
        var seasonal = new Category { Code = "CAT-008", Slug = "mevsim-ozelleri", Name = "Mevsim Özelleri", Description = "Mevsimden ilham alan, sınırlı süreli içecek ve tatlılar.", Icon = "bi-flower1", ImageUrl = ImageUrl("1512568400610-62da28bc8a13"), DisplayOrder = 8, IsActive = false, CreatedAt = new DateTime(2026, 9, 30) };
        context.Categories.Add(seasonal);

        // 2) Ürünler: CreateProduct(kategori, ad, fiyat, resim, açıklama, rozet, vejetaryen mi, özelleştirilebilir mi)
        context.Products.Add(CreateProduct(hot, "Klasik Cappuccino", 145, "1572442388796-11668a67e53d", "Kadifemsi süt köpüğünün altında yoğun duble espresso.", null, true, true));
        context.Products.Add(CreateProduct(hot, "Vanilyalı Latte", 160, "1509042239860-f550ce710b93", "Madagaskar vanilyasıyla yumuşak espresso ve buharda ısıtılmış süt.", "Çok Satan", true, true));
        context.Products.Add(CreateProduct(hot, "Karamel Macchiato", 170, "1485808191679-5f86510681a2", "Espressoyla işaretlenmiş vanilyalı süt ve tereyağlı karamel sos.", "Popüler", true, true));
        context.Products.Add(CreateProduct(iced, "Buzlu İspanyol Latte", 175, "1461023058943-07fcbe16d735", "Buz üzerinde espresso ve kremamsı yoğunlaştırılmış süt tatlılığı.", "Çok Satan", true, true));
        context.Products.Add(CreateProduct(iced, "Buzlu Mocha", 180, "1517701604599-bb29b565090c", "Duble espresso, Belçika çikolatası ve buz üzerinde soğuk süt.", null, true, true));
        context.Products.Add(CreateProduct(iced, "Cold Brew", 155, "1534778101976-62847782c213", "18 saat yavaş demlenir. Yumuşak, yoğun ve doğal tatlı.", null, true, false));
        context.Products.Add(CreateProduct(signature, "Çilekli Bulut Latte", 205, "1572490122747-3968b75cc699", "Taze çilekli süt, espresso ve pembe soğuk köpük bulutu.", "Çok Satan", true, true));
        context.Products.Add(CreateProduct(signature, "Güllü Matcha", 210, "1515823064-d6e0c04616a7", "Seremoni matchası, hafif gül şurubu ve yulaf sütüyle.", "Yeni", true, true));
        context.Products.Add(CreateProduct(signature, "Pembe Orman Meyveli Frappe", 215, "1553530666-ba11a7da3888", "Ahududu, çilek ve vanilyalı krema karışımı, üzeri çırpılmış kremalı.", "Popüler", true, false));
        context.Products.Add(CreateProduct(cakes, "Pembe Kadife Pasta", 225, "1578985545062-69928b1d9587", "Vanilyalı krem peynir kremasıyla katlanmış gül tonlu pandispanya.", "Şefin Seçimi", true, false));
        context.Products.Add(CreateProduct(cakes, "Lotus Cheesecake", 245, "1533134242443-d4fd215305ad", "Biscoff tabanlı fırın cheesecake, üzeri karamel sos.", "Çok Satan", true, false));
        context.Products.Add(CreateProduct(cakes, "Çilekli Pasta", 230, "1565958011703-44f9829ba187", "Hafif şifon kek, taze çilekler ve çırpılmış mascarpone.", "Yeni", true, false));
        context.Products.Add(CreateProduct(cakes, "Ahududulu Makaron", 180, "1569864358642-9d1684040f43", "Ekşimsi ahududu ganajlı dörtlü bademli makaron kutusu.", null, true, false));
        context.Products.Add(CreateProduct(bakery, "Tereyağlı Kruvasan", 95, "1555507036-ab1f4038808a", "Her sabah Fransız tereyağıyla katlanan, çıtır ve altın renkli.", "Popüler", true, false));
        context.Products.Add(CreateProduct(bakery, "Bademli Kruvasan", 125, "1623334044303-241021148842", "Badem kreması ve kavrulmuş badem dilimleriyle iki kez pişirilir.", null, true, false));
        context.Products.Add(CreateProduct(bakery, "Tarçınlı Rulo", 115, "1509365465985-25d11c17e812", "Tarçınlı şekerli yumuşak brioş, krem peynirli sosla.", "Yeni", true, false));
        context.Products.Add(CreateProduct(snacks, "Avokadolu Tost", 235, "1541519227354-08fa5d50c44d", "Ekşi maya ekmek, ezilmiş avokado, çeri domates ve pul biber.", "Şefin Seçimi", true, false));
        context.Products.Add(CreateProduct(snacks, "Bol Peynirli Tost", 210, "1528735602780-2552fd46c7af", "Tereyağlı kızarmış brioş arasında eriyen çedar ve mozzarella.", null, true, false));
        context.Products.Add(CreateProduct(snacks, "Hindi ve Brie Panini", 255, "1528736235302-52922df5c122", "Füme hindi, kremamsı brie ve incir reçeli, altın rengine kadar preslenir.", null, false, false));
        context.Products.Add(CreateProduct(snacks, "Tavuklu Sezar Dürüm", 245, "1626700051175-6818013e1d4f", "Izgara tavuk, çıtır marul, parmesan ve ev yapımı sezar sos.", null, false, false));

        // 3) Ürün seçenekleri: boyut, süt, ekstra shot, şurup, krema (özelleştirilebilir ürünlerin hepsi bunları kullanır)
        context.ProductOptions.Add(new ProductOption { GroupKey = OptionGroups.Size, Value = "Küçük", PriceDelta = 0 });
        context.ProductOptions.Add(new ProductOption { GroupKey = OptionGroups.Size, Value = "Orta", PriceDelta = 15, IsDefault = true });
        context.ProductOptions.Add(new ProductOption { GroupKey = OptionGroups.Size, Value = "Büyük", PriceDelta = 30 });
        context.ProductOptions.Add(new ProductOption { GroupKey = OptionGroups.Milk, Value = "Normal Süt", PriceDelta = 0, IsDefault = true });
        context.ProductOptions.Add(new ProductOption { GroupKey = OptionGroups.Milk, Value = "Yulaf Sütü", PriceDelta = 15 });
        context.ProductOptions.Add(new ProductOption { GroupKey = OptionGroups.Milk, Value = "Badem Sütü", PriceDelta = 15 });
        context.ProductOptions.Add(new ProductOption { GroupKey = OptionGroups.ExtraShot, Value = "Evet", PriceDelta = 20 });
        context.ProductOptions.Add(new ProductOption { GroupKey = OptionGroups.Syrup, Value = "Vanilya Şurubu", PriceDelta = 10 });
        context.ProductOptions.Add(new ProductOption { GroupKey = OptionGroups.Syrup, Value = "Karamel Şurubu", PriceDelta = 10 });
        context.ProductOptions.Add(new ProductOption { GroupKey = OptionGroups.Cream, Value = "Krema", PriceDelta = 15 });

        // 4) QR kodlu masalar
        context.Tables.Add(new Table { Number = 7, Area = "Bahçe Köşesi", QrToken = "CF-T07-GUL", IsActive = true });
        context.Tables.Add(new Table { Number = 12, Area = "Pencere Önü", QrToken = "CF-T12-SEFTALI", IsActive = true });

        await context.SaveChangesAsync();
    }

    /// <summary>Örnek ürün nesnesini oluşturur.</summary>
    private static Product CreateProduct(Category category, string name, decimal price, string imageId,
        string description, string? badge, bool isVegetarian, bool isCustomizable)
    {
        Product product = new Product();
        product.Category = category;
        product.Name = name;
        product.BasePrice = price;
        product.ImageUrl = ImageUrl(imageId);
        product.Description = description;
        product.Badge = badge;
        product.IsVegetarian = isVegetarian;
        product.IsCustomizable = isCustomizable;
        product.IsActive = true;
        return product;
    }

    /// <summary>Unsplash fotoğraf numarasından resim adresi üretir.</summary>
    private static string ImageUrl(string imageId)
    {
        return "https://images.unsplash.com/photo-" + imageId + "?auto=format&fit=crop&w=700&q=80";
    }
}
