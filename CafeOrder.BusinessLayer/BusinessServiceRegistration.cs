using CafeOrder.BusinessLayer.Creation;
using CafeOrder.BusinessLayer.Events;
using CafeOrder.BusinessLayer.Mapping;
using CafeOrder.BusinessLayer.Pricing;
using CafeOrder.BusinessLayer.Pricing.Fees;
using CafeOrder.BusinessLayer.Services.Account;
using CafeOrder.BusinessLayer.Services.Cart;
using CafeOrder.BusinessLayer.Services.Categories;
using CafeOrder.BusinessLayer.Services.Customers;
using CafeOrder.BusinessLayer.Services.Dashboard;
using CafeOrder.BusinessLayer.Services.Notifications;
using CafeOrder.BusinessLayer.Services.Orders;
using CafeOrder.BusinessLayer.Services.Products;
using CafeOrder.BusinessLayer.Services.Tables;
using CafeOrder.BusinessLayer.States;
using CafeOrder.BusinessLayer.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CafeOrder.BusinessLayer;

/// <summary>
/// İş katmanının DI kayıtları. Program.cs sadece builder.Services.AddBusinessLayer(...) çağırır.
/// Yeni bir strateji, creator, doğrulama halkası veya dinleyici eklendiğinde kaydı buraya yazılır.
/// </summary>
public static class BusinessServiceRegistration
{
    public static IServiceCollection AddBusinessLayer(this IServiceCollection services, IConfiguration configuration)
    {
        // ---- Ayarlar (Options pattern): appsettings.json → "Pricing". Geçersiz değerle uygulama açılmaz.
        services.AddOptions<PricingOptions>()
            .Bind(configuration.GetSection(PricingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // ---- Zaman: DateTime.Now yerine TimeProvider. Testte sahte saat verilebilir.
        services.TryAddSingleton(TimeProvider.System);

        // ---- AutoMapper: bu projedeki bütün Profile sınıfları (Mapping klasöründeki *Mappings) taranır.
        // Uygulama açılırken her Profile'ın constructor'ı bir kez çalışır ve CreateMap kuralları kaydedilir.
        // Servisler constructor'da IMapper alır ve _mapper.Map<T>(...) ile eşleme yapar.
        services.AddAutoMapper(typeof(CategoryMappings).Assembly);

        // ---- DECORATOR: birim fiyat hesabı. Decorator zinciri bu sınıfın içinde kuruluyor.
        services.AddSingleton<IUnitPriceCalculator, DecoratorUnitPriceCalculator>();

        // ---- STRATEGY: kanal başına bir ücret kuralı. OrderPricingService hepsini alır, Channel'a göre seçer.
        services.AddSingleton<IDeliveryFeeStrategy, DeliveryFeeStrategy>();
        services.AddSingleton<IDeliveryFeeStrategy, PickupFeeStrategy>();
        services.AddSingleton<IDeliveryFeeStrategy, TableFeeStrategy>();

        // ---- STATE: durum başına bir sınıf. IOrderStateProvider hepsini alır, Status'a göre verir.
        services.AddSingleton<OrderState, ReceivedState>();
        services.AddSingleton<OrderState, PreparingState>();
        services.AddSingleton<OrderState, ReadyState>();
        services.AddSingleton<OrderState, DeliveredState>();
        services.AddSingleton<OrderState, CancelledState>();
        services.AddSingleton<IOrderStateProvider, OrderStateProvider>();
        services.AddSingleton<IOrderStatusRecorder, OrderStatusRecorder>();   // durum geçmişi satırları

        // ---- FACTORY METHOD: kanal başına bir sipariş kurucu. OrderService hepsini alır, Channel'a göre seçer.
        // Creator'lar Order'ı AutoMapper ile DTO'dan oluşturur (IMapper thread-safe, singleton'da kullanılabilir).
        services.AddSingleton<OrderCreator, DeliveryOrderCreator>();
        services.AddSingleton<OrderCreator, PickupOrderCreator>();
        services.AddSingleton<OrderCreator, TableOrderCreator>();

        // ---- CHAIN OF RESPONSIBILITY: KAYIT SIRASI = ZİNCİR SIRASI.
        // Halkalar transient: her zincir kendi halkalarını alır (SetNext halkanın içini değiştirdiği için paylaşılmamalı).
        services.AddTransient<OrderValidationHandler, ProductHandler>();
        services.AddTransient<OrderValidationHandler, QuantityHandler>();
        services.AddTransient<OrderValidationHandler, ChannelInfoHandler>();
        services.AddTransient<OrderValidationHandler, OrderAmountHandler>();
        services.AddScoped<IOrderValidationChain, OrderValidationChain>();

        // ---- OBSERVER: sipariş olaylarını dinleyenler. Bildirimi ortak INotificationWriter yazar (repository kullandığı için scoped).
        services.AddScoped<INotificationWriter, NotificationWriter>();
        services.AddScoped<IOrderObserver, KitchenObserver>();
        services.AddScoped<IOrderObserver, CashierObserver>();
        services.AddScoped<IOrderObserver, AdminObserver>();
        services.AddScoped<IOrderEventPublisher, OrderEventPublisher>();

        // ---- Servisler
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ITableService, TableService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IOrderPricingService, OrderPricingService>();
        services.AddScoped<IOrderQueryService, OrderQueryService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IAccountService, AccountService>();

        return services;
    }
}
