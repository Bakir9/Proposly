using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.CompanyManagement.Enums;

namespace Proposly.Infrastructure.Persistence.Configurations;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.FiscalYearStartMonth).HasDefaultValue(1);

        // Plan
        builder.Property(c => c.PlanTier).HasConversion<string>().HasMaxLength(20).HasDefaultValue(PlanTier.Free);
        builder.Property(c => c.MaxUsers).HasDefaultValue(1);
        builder.Property(c => c.MaxProjects).HasDefaultValue(3);
        builder.Property(c => c.PlanExpiresAt);

        // VAT settings
        builder.Property(c => c.CompanyCountry).HasMaxLength(2);
        builder.Property(c => c.CompanyVatNumber).HasMaxLength(50);
        builder.Property(c => c.DefaultVatRate).HasColumnType("numeric(5,2)").HasDefaultValue(0m);
        builder.Property(c => c.VatExemptReason).HasMaxLength(500);

        builder.HasIndex(c => c.Name).IsUnique();
    }
}
