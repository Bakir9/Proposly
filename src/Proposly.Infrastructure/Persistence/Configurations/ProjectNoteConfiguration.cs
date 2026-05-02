using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proposly.Domain.ProjectManagement.Entities;

namespace Proposly.Infrastructure.Persistence.Configurations;

public sealed class ProjectNoteConfiguration : IEntityTypeConfiguration<ProjectNote>
{
    public void Configure(EntityTypeBuilder<ProjectNote> builder)
    {
        builder.ToTable("ProjectNotes");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();

        builder.Property(n => n.Title).HasMaxLength(500).IsRequired();
        builder.Property(n => n.Content).HasColumnType("text");
        builder.Property(n => n.AuthorName).HasMaxLength(200).IsRequired();
    }
}
