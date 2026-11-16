using CafeOrder.BusinessLayer;
using CafeOrder.DataAccessLayer;
using CafeOrder.DataAccessLayer.Context;
using CafeOrder.EntityLayer.Entities;
using CafeOrder.WebUI.Identity;
using CafeOrder.WebUI.ModelBinding;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Formdan gelen ondalık sayılar "185,50" ya da "185.50" yazılsa da doğru okunur (sunucunun diline bakılmaz).
builder.Services.AddControllersWithViews(options =>
    options.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider()));

// Katmanların kendi kayıtları: her katman neyi kaydettiğini kendisi bilir.
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("appsettings.json içinde ConnectionStrings:DefaultConnection yok.");
builder.Services.AddDataAccessLayer(connectionString);        // DbContext, Unit of Work, repository'ler
builder.Services.AddBusinessLayer(builder.Configuration);     // ayarlar, Mapster, desenler, servisler

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
    .AddErrorDescriber<TurkishIdentityErrorDescriber>()
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
