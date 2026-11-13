using CafeOrder.EntityLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CafeOrder.DataAccessLayer.Context.Configurations;

public class OrderDetailOptionConfiguration : IEntityTypeConfiguration<OrderDetailOption>
{
    /// <summary>OrderDetailOptions tablosu: seçenek ilişkisi Restrict (kullanılmış seçenek silinemez).</summary>
    public void Configure(EntityTypeBuilder<OrderDetailOption> builder)
    {
        builder.HasOne(x => x.ProductOption)
            .WithMany()
            .HasForeignKey(x => x.ProductOptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}