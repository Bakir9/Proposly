using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proposly.Domain.ProjectManagement.Entities;

namespace Proposly.Infrastructure.Persistence.Configurations;

public sealed class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> builder)
    {
        builder.ToTable("TimeEntries");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.HoursWorked).HasColumnType("numeric(8,2)").IsRequired();
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.TaskId);

        builder.OwnsOne(t => t.HourlyRateSnapshot, money =>
        {
            money.Property(x => x.Amount).HasColumnName("HourlyRateAmount").HasColumnType("numeric(18,2)").IsRequired();
            money.Property(x => x.Currency).HasColumnName("HourlyRateCurrency").HasMaxLength(3).IsRequired();
        });
    }
}
