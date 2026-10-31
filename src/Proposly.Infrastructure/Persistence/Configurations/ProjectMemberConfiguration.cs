using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proposly.Domain.ProjectManagement.Entities;

namespace Proposly.Infrastructure.Persistence.Configurations;

public sealed class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("ProjectMembers");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Name).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Role).HasMaxLength(100).IsRequired();

        builder.OwnsOne(m => m.HourlyRate, money =>
        {
            money.Property(x => x.Amount).HasColumnName("HourlyRateAmount").HasColumnType("numeric(18,2)").IsRequired();
            money.Property(x => x.Currency).HasColumnName("HourlyRateCurrency").HasMaxLength(3).IsRequired();
        });

        builder.OwnsOne(m => m.CostRate, money =>
        {
            money.Property(x => x.Amount).HasColumnName("CostRateAmount").HasColumnType("numeric(18,2)").IsRequired().HasDefaultValue(0m);
            money.Property(x => x.Currency).HasColumnName("CostRateCurrency").HasMaxLength(3).IsRequired().HasDefaultValue("EUR");
        });

        builder.OwnsOne(m => m.BillRate, money =>
        {
            money.Property(x => x.Amount).HasColumnName("BillRateAmount").HasColumnType("numeric(18,2)").IsRequired().HasDefaultValue(0m);
            money.Property(x => x.Currency).HasColumnName("BillRateCurrency").HasMaxLength(3).IsRequired().HasDefaultValue("EUR");
        });

        builder.Property(m => m.AllocatedHours).HasColumnType("numeric(9,2)").HasDefaultValue(0m);
    }
}
