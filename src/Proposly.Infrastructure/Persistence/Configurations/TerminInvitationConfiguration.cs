using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proposly.Domain.CalendarManagement.Entities;

namespace Proposly.Infrastructure.Persistence.Configurations;

public sealed class TerminInvitationConfiguration : IEntityTypeConfiguration<TerminInvitation>
{
    public void Configure(EntityTypeBuilder<TerminInvitation> builder)
    {
        builder.ToTable("TerminInvitations");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();
        builder.Property(i => i.InviteeName).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(i => i.ProposedMessage).HasMaxLength(500);

        builder.HasIndex(i => new { i.TerminId, i.InviteeId }).IsUnique();
    }
}
