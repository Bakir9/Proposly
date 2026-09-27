using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proposly.Domain.WorkTimeManagement.Entities;

namespace Proposly.Infrastructure.Persistence.Configurations;

public sealed class BreakRuleConfiguration : IEntityTypeConfiguration<BreakRule>
{
    public void Configure(EntityTypeBuilder<BreakRule> builder)
    {
        builder.ToTable("BreakRules");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.AboveHours).HasColumnType("numeric(5,2)");

        // One tier per threshold within a policy version.
        builder.HasIndex(r => new { r.WorkTimePolicyId, r.AboveHours }).IsUnique();
    }
}
