using DartERP.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartERP.Infrastructure.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.ProductId);

        builder.Property(p => p.SKU).IsRequired().HasMaxLength(30);
        builder.Property(p => p.ProductName).IsRequired().HasMaxLength(150);
        builder.Property(p => p.Category).HasConversion<string>().HasMaxLength(30);
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.UnitCost).HasColumnType("decimal(18,2)");
        builder.Property(p => p.SalePrice).HasColumnType("decimal(18,2)");

        builder.HasIndex(p => p.SKU).IsUnique();
    }
}
