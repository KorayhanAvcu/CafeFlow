using CafeOrder.EntityLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CafeOrder.DataAccessLayer.Context.Configurations;

/// <summary>Products tablosunun kuralları.</summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    /// <summary>Products tablosu: ad uzunluğu ve kategori ilişkisi Restrict (ürünü olan kategori silinemez).</summary>
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();

        // Ürünü olan kategori silinemesin.
        builder.HasOne(x => x.Category)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
