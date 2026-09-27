using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proposly.Domain.WorkTimeManagement.Entities;

namespace Proposly.Infrastructure.Persistence.Configurations;

public sealed class WorkTimePolicyConfiguration : IEntityTypeConfiguration<WorkTimePolicy>
{
    public void Configure(EntityTypeBuilder<WorkTimePolicy> builder)
    {
        builder.ToTable("WorkTimePolicies");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Jurisdiction).HasMaxLength(10).IsRequired();
        builder.Property(p => p.HolidayRegionCode).HasMaxLength(20);

        builder.Property(p => p.MaxHoursPerDay).HasColumnType("numeric(5,2)");
        builder.Property(p => p.MaxHoursPerWeek).HasColumnType("numeric(5,2)");
        builder.Property(p => p.MaxAverageHoursPerWeek).HasColumnType("numeric(5,2)");
        builder.Property(p => p.MinDailyRestHours).HasColumnType("numeric(5,2)");
        builder.Property(p => p.MinWeeklyRestHours).HasColumnType("numeric(5,2)");
        builder.Property(p => p.SurplusCapHours).HasColumnType("numeric(7,2)");
        builder.Property(p => p.DeficitFloorHours).HasColumnType("numeric(7,2)");

        builder.HasMany(p => p.BreakRules)
            .WithOne()
            .HasForeignKey(r => r.WorkTimePolicyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.BreakRules).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Only one version may start on a given date.
        builder.HasIndex(p => new { p.CompanyId, p.ValidFrom }).IsUnique();
    }
}
