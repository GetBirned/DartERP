using DartERP.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartERP.Infrastructure.Configurations;

public class QualityInspectionConfiguration : IEntityTypeConfiguration<QualityInspection>
{
    public void Configure(EntityTypeBuilder<QualityInspection> builder)
    {
        builder.HasKey(q => q.QualityInspectionId);

        builder.Property(q => q.Inspector).IsRequired().HasMaxLength(100);
        builder.Property(q => q.Result).HasConversion<string>().HasMaxLength(20);
        builder.Property(q => q.Notes).HasMaxLength(1000);

        builder.HasOne(q => q.SerializedItem)
            .WithMany(s => s.Inspections)
            .HasForeignKey(q => q.SerializedItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
