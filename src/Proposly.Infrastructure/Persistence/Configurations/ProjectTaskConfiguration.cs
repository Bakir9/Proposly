using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proposly.Domain.ProjectManagement.Entities;

namespace Proposly.Infrastructure.Persistence.Configurations;

public sealed class ProjectTaskConfiguration : IEntityTypeConfiguration<ProjectTask>
{
    public void Configure(EntityTypeBuilder<ProjectTask> builder)
    {
        builder.ToTable("ProjectTasks");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Title).HasMaxLength(300).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(50000);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.EstimatedHours).HasColumnType("numeric(8,2)");
        builder.Property(t => t.ActualHours).HasColumnType("numeric(8,2)");
        builder.Property(t => t.StartDate);
        builder.Property(t => t.CompletedAt);
        builder.Property(t => t.AssignedMemberId);

        builder.HasMany(t => t.Comments)
            .WithOne()
            .HasForeignKey(c => c.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.BlockedBy)
            .WithMany()
            .UsingEntity("ProjectTaskDependencies",
                l => l.HasOne(typeof(ProjectTask)).WithMany().HasForeignKey("BlockingTaskId").OnDelete(DeleteBehavior.Cascade),
                r => r.HasOne(typeof(ProjectTask)).WithMany().HasForeignKey("BlockedTaskId").OnDelete(DeleteBehavior.Cascade));

        builder.Navigation(t => t.Comments).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(t => t.BlockedBy).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
