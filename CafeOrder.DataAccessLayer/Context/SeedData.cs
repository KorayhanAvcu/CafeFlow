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
    public static async Task EnsureSeededAsync(CafeOrderDbContext context)
    {
        // Daha önce veri eklenmişse tekrar ekleme.
        bool hasData = await context.Categories.AnyAsync();
        if (hasData)
        {
            return;
        }

        // 1) Kategoriler
        var hot = new Category { Code = "CAT-001", Slug = "hot", Name = "Hot Coffees", Description = "Velvety lattes, bold espressos and comforting signature brews.", Icon = "bi-cup-hot", ImageUrl = ImageUrl("1509042239860-f550ce710b93"), DisplayOrder = 1, IsActive = true, CreatedAt = new DateTime(2026, 1, 12) };
        context.Categories.Add(hot);
        var iced = new Category { Code = "CAT-002", Slug = "iced", Name = "Iced Coffees", Description = "Chilled espresso drinks and slow-steeped cold brew over ice.", Icon = "bi-cup-straw", ImageUrl = ImageUrl("1461023058943-07fcbe16d735"), DisplayOrder = 2, IsActive = true, CreatedAt = new DateTime(2026, 1, 12) };
        context.Categories.Add(iced);
        var signature = new Category { Code = "CAT-003", Slug = "signature", Name = "Signature Drinks", Description = "Our pink house favorites: strawberry, rose and berry creations.", Icon = "bi-stars", ImageUrl = ImageUrl("1572490122747-3968b75cc699"), DisplayOrder = 3, IsActive = true, CreatedAt = new DateTime(2026, 2, 3) };
        context.Categories.Add(signature);
        var cakes = new Category { Code = "CAT-004", Slug = "cakes", Name = "Cakes & Desserts", Description = "Cakes, cheesecakes and macarons baked fresh every morning.", Icon = "bi-cake2", ImageUrl = ImageUrl("1578985545062-69928b1d9587"), DisplayOrder = 4, IsActive = true, CreatedAt = new DateTime(2026, 2, 3) };
        context.Categories.Add(cakes);
        var bakery = new Category { Code = "CAT-005", Slug = "bakery", Name = "Bakery", Description = "Buttery croissants, cinnamon rolls and morning pastries.", Icon = "bi-basket", ImageUrl = ImageUrl("1555507036-ab1f4038808a"), DisplayOrder = 5, IsActive = true, CreatedAt = new DateTime(2026, 3, 18) };
        context.Categories.Add(bakery);
        var snacks = new Category { Code = "CAT-006", Slug = "snacks", Name = "Snacks", Description = "Toasts, sandwiches and light bites to enjoy any time of day.", Icon = "bi-egg-fried", ImageUrl = ImageUrl("1541519227354-08fa5d50c44d"), DisplayOrder = 6, IsActive = true, CreatedAt = new DateTime(2026, 4, 7) };
        context.Categories.Add(snacks);
        var tea = new Category { Code = "CAT-007", Slug = "tea", Name = "Tea & Infusions", Description = "Loose-leaf teas, herbal infusions and matcha alternatives.", Icon = "bi-cup", ImageUrl = ImageUrl("1544787219-7f47ccb76574"), DisplayOrder = 7, IsActive = true, CreatedAt = new DateTime(2026, 8, 20) };
        context.Categories.Add(tea);
        var seasonal = new Category { Code = "CAT-008", Slug = "seasonal", Name = "Seasonal Specials", Description = "Limited-time drinks and treats inspired by the season.", Icon = "bi-flower1", ImageUrl = ImageUrl("1512568400610-62da28bc8a13"), DisplayOrder = 8, IsActive = false, CreatedAt = new DateTime(2026, 9, 30) };
        context.Categories.Add(seasonal);

        // 2) Ürünler: CreateProduct(kategori, ad, fiyat, resim, açıklama, rozet, vejetaryen mi, özelleştirilebilir mi)
        context.Products.Add(CreateProduct(hot, "Classic Cappuccino", 145, "1572442388796-11668a67e53d", "Rich double espresso under a deep layer of velvety milk foam.", null, true, true));
        context.Products.Add(CreateProduct(hot, "Vanilla Latte", 160, "1509042239860-f550ce710b93", "Smooth espresso and steamed milk with Madagascar vanilla.", "Best Seller", true, true));
        context.Products.Add(CreateProduct(hot, "Caramel Macchiato", 170, "1485808191679-5f86510681a2", "Vanilla milk marked with espresso and a buttery caramel drizzle.", "Popular", true, true));
        context.Products.Add(CreateProduct(iced, "Iced Spanish Latte", 175, "1461023058943-07fcbe16d735", "Espresso over ice with creamy condensed milk sweetness.", "Best Seller", true, true));
        context.Products.Add(CreateProduct(iced, "Iced Mocha", 180, "1517701604599-bb29b565090c", "Double espresso, Belgian chocolate and cold milk over ice.", null, true, true));
        context.Products.Add(CreateProduct(iced, "Cold Brew", 155, "1534778101976-62847782c213", "Slow-steeped for 18 hours. Smooth, bold and naturally sweet.", null, true, false));
        context.Products.Add(CreateProduct(signature, "Strawberry Cloud Latte", 205, "1572490122747-3968b75cc699", "Fresh strawberry milk, espresso and a cloud of pink cold foam.", "Best Seller", true, true));
        context.Products.Add(CreateProduct(signature, "Rose Matcha", 210, "1515823064-d6e0c04616a7", "Ceremonial matcha with a delicate rose syrup and oat milk.", "New", true, true));
        context.Products.Add(CreateProduct(signature, "Pink Berry Frappe", 215, "1553530666-ba11a7da3888", "Blended raspberry, strawberry and vanilla cream with whipped top.", "Popular", true, false));
        context.Products.Add(CreateProduct(cakes, "Pink Velvet Cake", 225, "1578985545062-69928b1d9587", "Rose-tinted sponge layered with vanilla cream cheese frosting.", "Chef's Choice", true, false));
        context.Products.Add(CreateProduct(cakes, "Lotus Cheesecake", 245, "1533134242443-d4fd215305ad", "Baked cheesecake on a Biscoff crumb with caramel drizzle.", "Best Seller", true, false));
        context.Products.Add(CreateProduct(cakes, "Strawberry Shortcake", 230, "1565958011703-44f9829ba187", "Light chiffon, fresh strawberries and whipped mascarpone.", "New", true, false));
        context.Products.Add(CreateProduct(cakes, "Raspberry Macarons", 180, "1569864358642-9d1684040f43", "Box of four almond macarons with tangy raspberry ganache.", null, true, false));
        context.Products.Add(CreateProduct(bakery, "Butter Croissant", 95, "1555507036-ab1f4038808a", "Flaky, golden and laminated with French butter every morning.", "Popular", true, false));
        context.Products.Add(CreateProduct(bakery, "Almond Croissant", 125, "1623334044303-241021148842", "Twice-baked with almond frangipane and toasted almond flakes.", null, true, false));
        context.Products.Add(CreateProduct(bakery, "Cinnamon Roll", 115, "1509365465985-25d11c17e812", "Soft brioche swirl with cinnamon sugar and cream cheese glaze.", "New", true, false));
        context.Products.Add(CreateProduct(snacks, "Avocado Toast", 235, "1541519227354-08fa5d50c44d", "Sourdough, smashed avocado, cherry tomatoes and chili flakes.", "Chef's Choice", true, false));
        context.Products.Add(CreateProduct(snacks, "Grilled Cheese Sandwich", 210, "1528735602780-2552fd46c7af", "Cheddar and mozzarella melted on buttery toasted brioche.", null, true, false));
        context.Products.Add(CreateProduct(snacks, "Turkey & Brie Panini", 255, "1528736235302-52922df5c122", "Smoked turkey, creamy brie and fig jam, pressed until golden.", null, false, false));
        context.Products.Add(CreateProduct(snacks, "Chicken Caesar Wrap", 245, "1626700051175-6818013e1d4f", "Grilled chicken, crisp romaine, parmesan and house Caesar dressing.", null, false, false));

        // 3) Ürün seçenekleri: boyut, süt, ekstra shot, şurup (özelleştirilebilir ürünlerin hepsi bunları kullanır)
        context.ProductOptions.Add(new ProductOption { GroupKey = OptionGroups.Size, Value = "Small", PriceDelta = 0 });
        context.ProductOptions.Add(new ProductOption { GroupKey = OptionGroups.Size, Value = "Medium", PriceDelta = 15, IsDefault = true });
        context.ProductOptions.Add(new ProductOption { GroupKey = OptionGroups.Size, Value = "Large", PriceDelta = 30 });
        context.ProductOptions.Add(new ProductOption { GroupKey = OptionGroups.Milk, Value = "Regular", PriceDelta = 0, IsDefault = true });
        context.ProductOptions.Add(new ProductOption { GroupKey = OptionGroups.Milk, Value = "Oat", PriceDelta = 15 });
        context.ProductOptions.Add(new ProductOption { GroupKey = OptionGroups.Milk, Value = "Almond", PriceDelta = 15 });
        context.ProductOptions.Add(new ProductOption { GroupKey = OptionGroups.ExtraShot, Value = "Yes", PriceDelta = 20 });
        context.ProductOptions.Add(new ProductOption { GroupKey = OptionGroups.Syrup, Value = "Vanilla", PriceDelta = 10 });
        context.ProductOptions.Add(new ProductOption { GroupKey = OptionGroups.Syrup, Value = "Caramel", PriceDelta = 10 });

        // 4) QR kodlu masalar
        context.Tables.Add(new Table { Number = 7, Area = "Garden Corner", QrToken = "CF-T07-ROSE", IsActive = true });
        context.Tables.Add(new Table { Number = 12, Area = "Window Seats", QrToken = "CF-T12-PEACH", IsActive = true });

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
