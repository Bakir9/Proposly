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

        // Flags enum stored as an int — a set of at most seven bits, read on every target-hour
        // calculation, so not worth a child table or a string conversion.
        builder.Property(t => t.WorkingDays).HasConversion<int>();

        // One version per employee per start date.
        builder.HasIndex(t => new { t.CompanyId, t.UserId, t.ValidFrom }).IsUnique();
    }
}
