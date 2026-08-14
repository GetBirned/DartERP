using DartERP.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartERP.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.UserId);

        builder.Property(u => u.Username).HasMaxLength(50);
        builder.HasIndex(u => u.Username).IsUnique();

        builder.Property(u => u.Email).HasMaxLength(200);
        builder.Property(u => u.PasswordHash).HasMaxLength(300);
        builder.Property(u => u.DisplayName).HasMaxLength(100);
        builder.Property(u => u.Role).HasMaxLength(100);
        builder.Property(u => u.Phone).HasMaxLength(30);
        builder.Property(u => u.ProfilePicturePath).HasMaxLength(300);
    }
}
