using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proposly.Domain.CompanyManagement.Entities;

namespace Proposly.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.Email).HasMaxLength(200).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        builder.Property(u => u.IsDisabled).HasDefaultValue(false);
        builder.Property(u => u.InviteToken).HasMaxLength(100);
        builder.Property(u => u.InviteTokenExpiry);
        builder.Property(u => u.PasswordResetToken).HasMaxLength(100);
        builder.Property(u => u.PasswordResetTokenExpiry);

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.CompanyId);
    }
}
