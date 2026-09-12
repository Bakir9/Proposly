using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proposly.Domain.WorkTimeManagement.Entities;

namespace Proposly.Infrastructure.Persistence.Configurations;

public sealed class NonWorkingDayConfiguration : IEntityTypeConfiguration<NonWorkingDay>
{
    public void Configure(EntityTypeBuilder<NonWorkingDay> builder)
    {
        builder.ToTable("NonWorkingDays");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.Name).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.Source).HasConversion<string>().HasMaxLength(20);

        // One non-working day per date. This is also what delivers the spec's "counted once, not
        // twice" behaviour when a closure would otherwise overlap a holiday.
        builder.HasIndex(d => new { d.CompanyId, d.Date }).IsUnique();
    }
}
