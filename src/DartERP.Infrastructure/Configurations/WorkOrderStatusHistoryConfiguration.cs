using DartERP.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartERP.Infrastructure.Configurations;

public class WorkOrderStatusHistoryConfiguration : IEntityTypeConfiguration<WorkOrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<WorkOrderStatusHistory> builder)
    {
        builder.HasKey(h => h.WorkOrderStatusHistoryId);

        builder.Property(h => h.FromStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.ToStatus).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(h => h.WorkOrder)
            .WithMany(wo => wo.StatusHistory)
            .HasForeignKey(h => h.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict, not Cascade — same reasoning as PurchaseOrderStatusHistory.ChangedByUser.
        builder.HasOne(h => h.ChangedByUser)
            .WithMany()
            .HasForeignKey(h => h.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
