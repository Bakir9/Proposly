using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proposly.Domain.WorkTimeManagement.Entities;

namespace Proposly.Infrastructure.Persistence.Configurations;

public sealed class TimesheetBreachConfiguration : IEntityTypeConfiguration<TimesheetBreach>
{
    public void Configure(EntityTypeBuilder<TimesheetBreach> builder)
    {
        builder.ToTable("TimesheetBreaches");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.Kind).HasConversion<string>().HasMaxLength(30);
        builder.Property(b => b.LimitValue).HasColumnType("numeric(7,2)");
        builder.Property(b => b.ActualValue).HasColumnType("numeric(7,2)");

        builder.HasIndex(b => b.TimesheetId);
    }
}
