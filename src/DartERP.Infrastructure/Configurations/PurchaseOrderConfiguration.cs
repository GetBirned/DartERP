using DartERP.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartERP.Infrastructure.Configurations;

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.HasKey(po => po.PurchaseOrderId);

        builder.Property(po => po.PurchaseOrderNumber).IsRequired().HasMaxLength(20);
        builder.Property(po => po.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(po => po.Notes).HasMaxLength(1000);
        builder.Property(po => po.TotalAmount).HasColumnType("decimal(18,2)");

        builder.HasIndex(po => po.PurchaseOrderNumber).IsUnique();

        builder.HasOne(po => po.Vendor)
            .WithMany(v => v.PurchaseOrders)
            .HasForeignKey(po => po.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(po => po.Lines)
            .WithOne(l => l.PurchaseOrder)
            .HasForeignKey(l => l.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
