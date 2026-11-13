using CafeOrder.EntityLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CafeOrder.DataAccessLayer.Context.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    /// <summary>Notifications tablosu: hedef enum'u metin olarak saklanır, mesaj en fazla 500 karakter.</summary>
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(x => x.Target).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Message).HasMaxLength(500).IsRequired();
    }
}