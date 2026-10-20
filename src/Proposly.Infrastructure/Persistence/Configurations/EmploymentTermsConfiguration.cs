using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proposly.Domain.WorkTimeManagement.Entities;

namespace Proposly.Infrastructure.Persistence.Configurations;

public sealed class EmploymentTermsConfiguration : IEntityTypeConfiguration<EmploymentTerms>
{
    public void Configure(EntityTypeBuilder<EmploymentTerms> builder)
    {
        builder.ToTable("EmploymentTerms");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.WeeklyHours).HasColumnType("numeric(5,2)");
        builder.Property(t => t.AnnualVacationDays).HasColumnType("numeric(5,1)");

        builder.Property(t => t.EmploymentType)
            .HasConversion<string>().HasMaxLength(30)
            .HasDefaultValue(Domain.WorkTimeManagement.Enums.EmploymentType.FullTime);

        // Überstundenpauschale: null means no lump sum at all, which is not the same as zero.
        builder.Property(t => t.OvertimeLumpSumHours).HasColumnType("numeric(6,2)");

        // Flags enum stored as an int — a set of at most seven bits, read on every target-hour
        // calculation, so not worth a child table or a string conversion.
        builder.Property(t => t.WorkingDays).HasConversion<int>();

        // --- HR facet (Team/HR module). Defaults keep pre-existing rows valid. ---

        builder.Property(t => t.WorkModel)
            .HasConversion<string>().HasMaxLength(20)
            .HasDefaultValue(Domain.WorkTimeManagement.Enums.WorkModel.FullTime);

        builder.Property(t => t.OvertimeMode)
            .HasConversion<string>().HasMaxLength(20)
            .HasDefaultValue(Domain.WorkTimeManagement.Enums.OvertimeMode.TimeOff);

        foreach (var day in new[]
                 {
                     nameof(EmploymentTerms.MondayHours), nameof(EmploymentTerms.TuesdayHours),
                     nameof(EmploymentTerms.WednesdayHours), nameof(EmploymentTerms.ThursdayHours),
                     nameof(EmploymentTerms.FridayHours), nameof(EmploymentTerms.SaturdayHours),
                     nameof(EmploymentTerms.SundayHours),
                 })
            builder.Property<decimal?>(day).HasColumnType("numeric(4,2)");

        builder.Property(t => t.OvertimeSurchargePct).HasColumnType("numeric(5,1)");
        builder.Property(t => t.NightSurchargePct).HasColumnType("numeric(5,1)");
        builder.Property(t => t.SundaySurchargePct).HasColumnType("numeric(5,1)");
        builder.Property(t => t.HolidaySurchargePct).HasColumnType("numeric(5,1)");
        builder.Property(t => t.MaxHoursPerMonth).HasColumnType("numeric(5,1)");
        builder.Property(t => t.ExpectedWeeklyHours).HasColumnType("numeric(5,2)");
        builder.Property(t => t.HomeOfficeDaysPerWeek).HasColumnType("numeric(3,1)");
        builder.Property(t => t.MaxHoursPerDay).HasColumnType("numeric(4,2)");

        builder.Property(t => t.TrackingRequired).HasDefaultValue(true);
        builder.Property(t => t.AutoDeductBreak).HasDefaultValue(true);
        builder.Property(t => t.Flexitime).HasDefaultValue(false);
        builder.Property(t => t.OnCallDuty).HasDefaultValue(false);

        builder.OwnsOne(t => t.HourlyRate, m =>
        {
            m.Property(x => x.Amount).HasColumnName("HourlyRateAmount").HasColumnType("numeric(18,2)");
            m.Property(x => x.Currency).HasColumnName("HourlyRateCurrency").HasMaxLength(3);
        });

        // One version per employee per start date.
        builder.HasIndex(t => new { t.CompanyId, t.UserId, t.ValidFrom }).IsUnique();
    }
}
