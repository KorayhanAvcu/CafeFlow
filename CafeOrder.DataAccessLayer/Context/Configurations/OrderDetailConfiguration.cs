using CafeOrder.EntityLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CafeOrder.DataAccessLayer.Context.Configurations;

public class OrderDetailConfiguration : IEntityTypeConfiguration<OrderDetail>
{
    /// <summary>OrderDetails tablosu: ürün ilişkisi Restrict (siparişte geçen ürün silinemez).</summary>
    public void Configure(EntityTypeBuilder<OrderDetail> builder)
    {
        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}