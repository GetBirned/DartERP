using DartERP.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartERP.Infrastructure.Configurations;

public class PurchaseOrderAttachmentConfiguration : IEntityTypeConfiguration<PurchaseOrderAttachment>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderAttachment> builder)
    {
        builder.HasKey(a => a.PurchaseOrderAttachmentId);

        builder.Property(a => a.FileName).HasMaxLength(255);
        builder.Property(a => a.StoredPath).HasMaxLength(260);

        builder.HasOne(a => a.PurchaseOrder)
            .WithMany(po => po.Attachments)
            .HasForeignKey(a => a.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict, not Cascade — same reasoning as PurchaseOrderStatusHistory.ChangedByUser.
        builder.HasOne(a => a.UploadedByUser)
            .WithMany()
            .HasForeignKey(a => a.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
