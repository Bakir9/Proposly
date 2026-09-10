using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proposly.Domain.WorkTimeManagement.Entities;

namespace Proposly.Infrastructure.Persistence.Configurations;

public sealed class AbsenceEntitlementConfiguration : IEntityTypeConfiguration<AbsenceEntitlement>
{
    public void Configure(EntityTypeBuilder<AbsenceEntitlement> builder)
    {
        builder.ToTable("AbsenceEntitlements");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.EntitledDays).HasColumnType("numeric(5,1)");
        builder.Property(e => e.CarriedOverDays).HasColumnType("numeric(5,1)");
        builder.Property(e => e.UsedDays).HasColumnType("numeric(5,1)");

        // One entitlement row per employee per year.
        builder.HasIndex(e => new { e.CompanyId, e.UserId, e.Year }).IsUnique();
    }
}
