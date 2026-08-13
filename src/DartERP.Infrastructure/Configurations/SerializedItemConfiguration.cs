using DartERP.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartERP.Infrastructure.Configurations;

public class SerializedItemConfiguration : IEntityTypeConfiguration<SerializedItem>
{
    public void Configure(EntityTypeBuilder<SerializedItem> builder)
    {
        builder.HasKey(s => s.SerializedItemId);

        builder.Property(s => s.SerialNumber).IsRequired().HasMaxLength(40);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(s => s.SerialNumber).IsUnique();

        builder.HasOne(s => s.Product)
            .WithMany(p => p.SerializedItems)
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.WorkOrder)
            .WithMany(w => w.SerializedItems)
            .HasForeignKey(s => s.WorkOrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
