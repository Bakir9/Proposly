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
    }
}
