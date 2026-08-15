using DartERP.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartERP.Infrastructure.Configurations;

public class PurchaseOrderStatusHistoryConfiguration : IEntityTypeConfiguration<PurchaseOrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderStatusHistory> builder)
    {
        builder.HasKey(h => h.PurchaseOrderStatusHistoryId);

        builder.Property(h => h.FromStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.ToStatus).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(h => h.PurchaseOrder)
            .WithMany(po => po.StatusHistory)
            .HasForeignKey(h => h.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict, not Cascade — users are soft-deactivated via IsActive,
        // never hard-deleted, so this is a guard that should never actually
        // fire in practice, same reasoning as Disposition.CustomerId.
        builder.HasOne(h => h.ChangedByUser)
            .WithMany()
            .HasForeignKey(h => h.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
