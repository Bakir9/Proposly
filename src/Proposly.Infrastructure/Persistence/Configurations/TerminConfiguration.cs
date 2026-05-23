using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proposly.Domain.CalendarManagement.Entities;

namespace Proposly.Infrastructure.Persistence.Configurations;

public sealed class TerminConfiguration : IEntityTypeConfiguration<Termin>
{
    public void Configure(EntityTypeBuilder<Termin> builder)
    {
        builder.ToTable("Termins");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.Title).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(2000);
        builder.Property(t => t.Location).HasMaxLength(300);
        builder.Property(t => t.OrganizerName).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasMany(t => t.Invitations)
            .WithOne()
            .HasForeignKey(i => i.TerminId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
