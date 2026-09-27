using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proposly.Domain.WorkTimeManagement.Entities;

namespace Proposly.Infrastructure.Persistence.Configurations;

public sealed class WorkDayEntryConfiguration : IEntityTypeConfiguration<WorkDayEntry>
{
    public void Configure(EntityTypeBuilder<WorkDayEntry> builder)
    {
        builder.ToTable("WorkDayEntries");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.Note).HasMaxLength(500);

        // Derived on write and stored, so reports never recompute it.
        builder.Property(d => d.WorkedHours).HasColumnType("numeric(6,2)");

        // One entry per date within a timesheet — AddOrUpdateDay replaces rather than duplicates.
        builder.HasIndex(d => new { d.TimesheetId, d.Date }).IsUnique();
    }
}
