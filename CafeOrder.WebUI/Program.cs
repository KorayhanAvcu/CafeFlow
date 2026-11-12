using System.Globalization;
using CafeOrder.BusinessLayer.Events;
using CafeOrder.BusinessLayer.Mapping;
using CafeOrder.BusinessLayer.Pricing;
using CafeOrder.BusinessLayer.Services.Categories;
using CafeOrder.BusinessLayer.Services.Customers;
using CafeOrder.BusinessLayer.Services.Notifications;
using CafeOrder.BusinessLayer.Services.Orders;
using CafeOrder.BusinessLayer.Services.Products;
using CafeOrder.BusinessLayer.Services.Tables;
using CafeOrder.DataAccessLayer.Context;
using CafeOrder.DataAccessLayer.Repositories.Categories;
using CafeOrder.DataAccessLayer.Repositories.Customers;
using CafeOrder.DataAccessLayer.Repositories.Notifications;
using CafeOrder.DataAccessLayer.Repositories.Orders;
using CafeOrder.DataAccessLayer.Repositories.ProductOptions;
using CafeOrder.DataAccessLayer.Repositories.Products;
using CafeOrder.DataAccessLayer.Repositories.Tables;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.WebUI.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// Site İngilizce: fiyatlar "185.50" biçiminde yazılır ve formdan da öyle okunur.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-US");
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en-US");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<CafeOrderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session: müşterinin sepeti burada durur.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => options.IdleTimeout = TimeSpan.FromHours(2));

// Identity: admin girişi
builder.Services
    .AddIdentity<AppUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddEntityFrameworkStores<CafeOrderDbContext>()
    .AddClaimsPrincipalFactory<AppClaimsPrincipalFactory>();

// Giriş çerezi 30 gün geçerli; panel kullanıldıkça süresi uzar.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.LoginPath = "/admin/login";
    options.LogoutPath = "/admin/logout";
    options.AccessDeniedPath = "/admin/access-denied";
});

// Teslimat ücreti ayarları (appsettings.json → "Pricing")
builder.Services.AddSingleton(builder.Configuration.GetSection("Pricing").Get<PricingOptions>() ?? new PricingOptions());

// AutoMapper: Entity ↔ DTO eşlemeleri BusinessLayer/Mapping/GeneralMapping.cs içinde.
builder.Services.AddAutoMapper(typeof(GeneralMapping));

// Repository'ler
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductOptionRepository, ProductOptionRepository>();
builder.Services.AddScoped<ITableRepository, TableRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

// Servisler
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ITableService, TableService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// Observer: sipariş olaylarını dinleyenler
builder.Services.AddScoped<IOrderObserver, KitchenObserver>();
builder.Services.AddScoped<IOrderObserver, CashierObserver>();
builder.Services.AddScoped<IOrderObserver, AdminObserver>();
builder.Services.AddScoped<OrderEventPublisher>();

var app = builder.Build();

// Açılışta: veritabanını güncelle, boşsa örnek verileri ekle, ilk admin hesabını oluştur.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CafeOrderDbContext>();
    await context.Database.MigrateAsync();
    await SeedData.EnsureSeededAsync(context);
    await AdminAccountSeeder.EnsureSeededAsync(scope.ServiceProvider, app.Logger);
}

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
