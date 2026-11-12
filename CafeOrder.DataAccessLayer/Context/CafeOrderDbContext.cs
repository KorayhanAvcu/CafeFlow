using CafeOrder.EntityLayer.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CafeOrder.DataAccessLayer.Context;

/// <summary>
/// Veritabanı bağlantısı. IdentityDbContext, kafe tablolarına ek olarak
/// AspNetUsers, AspNetRoles gibi giriş tablolarını da getirir.
/// </summary>
public class CafeOrderDbContext : IdentityDbContext<AppUser>
{
#pragma warning disable CS8618 // DbSet'leri EF Core doldurur
    public CafeOrderDbContext(DbContextOptions<CafeOrderDbContext> options) : base(options)
    {
    }

    // Her DbSet bir tablodur.
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductOption> ProductOptions { get; set; }
    public DbSet<Table> Tables { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderDetail> OrderDetails { get; set; }
    public DbSet<OrderDetailOption> OrderDetailOptions { get; set; }
    public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }
    public DbSet<Notification> Notifications { get; set; }
#pragma warning restore CS8618

    /// <summary>Projedeki bütün decimal alanlar decimal(18,2) olsun.</summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Önce Identity kendi tablolarını kurar.
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>().Property(u => u.FullName).HasMaxLength(100);

        // Context/Configurations klasöründeki bütün XxxConfiguration sınıflarını uygular.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CafeOrderDbContext).Assembly);
    }
}
