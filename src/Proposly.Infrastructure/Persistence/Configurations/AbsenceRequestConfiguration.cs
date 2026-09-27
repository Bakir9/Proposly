using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proposly.Domain.WorkTimeManagement.Entities;

namespace Proposly.Infrastructure.Persistence.Configurations;

public sealed class AbsenceRequestConfiguration : IEntityTypeConfiguration<AbsenceRequest>
{
    public void Configure(EntityTypeBuilder<AbsenceRequest> builder)
    {
        builder.ToTable("AbsenceRequests");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.ConsumedDays).HasColumnType("numeric(5,1)");

        // Non-medical, employee-supplied note. There is deliberately no column for a diagnosis or
        // any other health detail — see AbsenceRequest.
        builder.Property(a => a.Reason).HasMaxLength(500);
        builder.Property(a => a.DecisionReason).HasMaxLength(500);

        // Overlap checks and the per-year list both filter on employee then date.
        builder.HasIndex(a => new { a.CompanyId, a.UserId, a.StartDate });
        builder.HasIndex(a => new { a.CompanyId, a.Status });
    }
}
