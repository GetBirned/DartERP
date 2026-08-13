using DartERP.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartERP.Infrastructure.Configurations;

public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.HasKey(v => v.VendorId);

        builder.Property(v => v.VendorNumber).IsRequired().HasMaxLength(20);
        builder.Property(v => v.CompanyName).IsRequired().HasMaxLength(150);
        builder.Property(v => v.ContactName).HasMaxLength(100);
        builder.Property(v => v.Email).HasMaxLength(150);
        builder.Property(v => v.Phone).HasMaxLength(30);
        builder.Property(v => v.VendorType).HasConversion<string>().HasMaxLength(30);

        builder.HasIndex(v => v.VendorNumber).IsUnique();
    }
}
