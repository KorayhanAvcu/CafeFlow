using CafeOrder.EntityLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CafeOrder.DataAccessLayer.Context.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    /// <summary>Orders tablosu: sipariş numarası benzersiz; kanal ve durum enum'ları metin olarak saklanır.</summary>
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(x => x.OrderNo).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.OrderNo).IsUnique();
        builder.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
    }
}