using CafeOrder.EntityLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CafeOrder.DataAccessLayer.Context.Configurations;

public class TableConfiguration : IEntityTypeConfiguration<Table>
{
    /// <summary>Tables tablosu: QR kodu zorunlu ve benzersiz.</summary>
    public void Configure(EntityTypeBuilder<Table> builder)
    {
        builder.Property(x => x.QrToken).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.QrToken).IsUnique();
    }
}