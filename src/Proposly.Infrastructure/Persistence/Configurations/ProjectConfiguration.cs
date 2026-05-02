using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proposly.Domain.ProjectManagement.Entities;

namespace Proposly.Infrastructure.Persistence.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.ClientName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

        builder.OwnsOne(p => p.Budget, m =>
        {
            m.Property(x => x.Amount).HasColumnName("BudgetAmount").HasColumnType("numeric(18,2)").IsRequired();
            m.Property(x => x.Currency).HasColumnName("BudgetCurrency").HasMaxLength(3).IsRequired();
        });

        builder.OwnsOne(p => p.OfferedAmount, m =>
        {
            m.Property(x => x.Amount).HasColumnName("OfferedAmount").HasColumnType("numeric(18,2)");
            m.Property(x => x.Currency).HasColumnName("OfferedCurrency").HasMaxLength(3);
        });

        builder.HasMany(p => p.Members)
            .WithOne()
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Tasks)
            .WithOne()
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Milestones)
            .WithOne()
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Expenses)
            .WithOne()
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.TimeEntries)
            .WithOne()
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Notes)
            .WithOne()
            .HasForeignKey(n => n.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Members).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.Tasks).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.Milestones).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.Expenses).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.TimeEntries).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.Notes).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(p => p.CompanyId);
    }
}
