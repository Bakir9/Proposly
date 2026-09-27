using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proposly.Domain.WorkTimeManagement.Entities;

namespace Proposly.Infrastructure.Persistence.Configurations;

public sealed class TimesheetConfiguration : IEntityTypeConfiguration<Timesheet>
{
    public void Configure(EntityTypeBuilder<Timesheet> builder)
    {
        builder.ToTable("Timesheets");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);

        // Month-end figures, frozen at approval.
        builder.Property(t => t.TargetHoursSnapshot).HasColumnType("numeric(7,2)");
        builder.Property(t => t.ActualHoursSnapshot).HasColumnType("numeric(7,2)");
        builder.Property(t => t.OpeningBalanceHours).HasColumnType("numeric(7,2)");
        builder.Property(t => t.ClosingBalanceHours).HasColumnType("numeric(7,2)");
        builder.Property(t => t.ForfeitedHours).HasColumnType("numeric(7,2)");
        builder.Property(t => t.AbsorbedByLumpSumHours).HasColumnType("numeric(7,2)");
        builder.Property(t => t.CoveredByAllInHours).HasColumnType("numeric(7,2)");

        builder.HasMany(t => t.Days)
            .WithOne()
            .HasForeignKey(d => d.TimesheetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(t => t.Days).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(t => t.Breaches)
            .WithOne()
            .HasForeignKey(b => b.TimesheetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(t => t.Breaches).UsePropertyAccessMode(PropertyAccessMode.Field);

        // One timesheet per employee per month.
        builder.HasIndex(t => new { t.CompanyId, t.UserId, t.Year, t.Month }).IsUnique();

        // Supports the approver queues and the company month overview.
        builder.HasIndex(t => new { t.CompanyId, t.Year, t.Month });
        builder.HasIndex(t => new { t.CompanyId, t.Status });
    }
}
