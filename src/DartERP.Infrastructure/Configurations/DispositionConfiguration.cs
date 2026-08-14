using DartERP.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartERP.Infrastructure.Configurations;

public class DispositionConfiguration : IEntityTypeConfiguration<Disposition>
{
    public void Configure(EntityTypeBuilder<Disposition> builder)
    {
        builder.HasKey(d => d.DispositionId);

        builder.Property(d => d.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.Notes).HasMaxLength(1000);

        builder.HasOne(d => d.SerializedItem)
            .WithMany(s => s.Dispositions)
            .HasForeignKey(d => d.SerializedItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Optional recipient — Restrict rather than Cascade/SetNull since a
        // customer is soft-deactivated, never hard-deleted, so this should
        // never actually fire; it's a guard, not an expected path.
        builder.HasOne(d => d.Customer)
            .WithMany()
            .HasForeignKey(d => d.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
