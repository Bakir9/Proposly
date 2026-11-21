using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proposly.Domain.ProjectManagement.Entities;

namespace Proposly.Infrastructure.Persistence.Configurations;

public sealed class TaskTemplateConfiguration : IEntityTypeConfiguration<TaskTemplate>
{
    public void Configure(EntityTypeBuilder<TaskTemplate> builder)
    {
        builder.ToTable("TaskTemplates");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(1000);

        builder.HasMany(t => t.Steps)
            .WithOne()
            .HasForeignKey(s => s.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(t => t.Steps).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class TaskTemplateStepConfiguration : IEntityTypeConfiguration<TaskTemplateStep>
{
    public void Configure(EntityTypeBuilder<TaskTemplateStep> builder)
    {
        builder.ToTable("TaskTemplateSteps");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Title).HasMaxLength(200).IsRequired();
        builder.Property(s => s.EstimatedHours).HasColumnType("numeric(9,2)");
    }
}
