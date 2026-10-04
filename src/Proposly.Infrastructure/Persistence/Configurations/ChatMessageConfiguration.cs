using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Proposly.Domain.Chat.Entities;

namespace Proposly.Infrastructure.Persistence.Configurations;

public sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("ChatMessages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.AuthorName).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Body).HasMaxLength(4000);

        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // The thread is paged and unread counts are computed on this pair.
        builder.HasIndex(m => new { m.ConversationId, m.CreatedAt });
    }
}

public sealed class ChatAttachmentConfiguration : IEntityTypeConfiguration<ChatAttachment>
{
    public void Configure(EntityTypeBuilder<ChatAttachment> builder)
    {
        builder.ToTable("ChatAttachments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.FileName).IsRequired().HasMaxLength(255);
        builder.Property(a => a.ContentType).IsRequired().HasMaxLength(200);
        builder.Property(a => a.StorageKey).IsRequired().HasMaxLength(100);

        builder.HasOne<ChatMessage>()
            .WithMany()
            .HasForeignKey(a => a.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.MessageId);
        builder.HasIndex(a => new { a.ConversationId, a.CreatedAt });
    }
}
