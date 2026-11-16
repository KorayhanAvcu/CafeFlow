using CafeOrder.DataAccessLayer.Context;
using CafeOrder.DataAccessLayer.Repositories.Categories;
using CafeOrder.DataAccessLayer.Repositories.Customers;
using CafeOrder.DataAccessLayer.Repositories.Notifications;
using CafeOrder.DataAccessLayer.Repositories.Orders;
using CafeOrder.DataAccessLayer.Repositories.ProductOptions;
using CafeOrder.DataAccessLayer.Repositories.Products;
using CafeOrder.DataAccessLayer.Repositories.Tables;
using CafeOrder.DataAccessLayer.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CafeOrder.DataAccessLayer;

/// <summary>Veri katmanının DI kayıtları. Program.cs sadece builder.Services.AddDataAccessLayer(...) çağırır.</summary>
public static class DataAccessServiceRegistration
{
    /// <summary>DbContext, Unit of Work ve bütün repository'leri kaydeder. Hepsi scoped: bir istek = bir DbContext.</summary>
    public static IServiceCollection AddDataAccessLayer(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<CafeOrderDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductOptionRepository, ProductOptionRepository>();
        services.AddScoped<ITableRepository, TableRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        return services;
    }
}
