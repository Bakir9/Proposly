using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proposly.Domain.OfferManagement.Entities;

namespace Proposly.Infrastructure.Persistence.Configurations;

public sealed class ClientNoteConfiguration : IEntityTypeConfiguration<ClientNote>
{
    public void Configure(EntityTypeBuilder<ClientNote> builder)
    {
        builder.ToTable("ClientNotes");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();

        builder.Property(n => n.Content).HasColumnType("text").IsRequired();
        builder.Property(n => n.AuthorName).HasMaxLength(200).IsRequired();

        builder.HasIndex(n => n.ClientId);
    }
}
