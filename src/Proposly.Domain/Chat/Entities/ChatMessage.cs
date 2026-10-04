using Proposly.Shared.Interfaces;
using Proposly.Shared.Primitives;

namespace Proposly.Domain.Chat.Entities;

public sealed class ChatMessage : Entity<Guid>, ITenantEntity
{
    private ChatMessage() { } // For EF Core

    private ChatMessage(Guid id, Guid companyId, Guid conversationId, Guid authorUserId, string authorName, string? body)
        : base(id)
    {
        CompanyId = companyId;
        ConversationId = conversationId;
        AuthorUserId = authorUserId;
        AuthorName = authorName;
        Body = body;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// <paramref name="authorName"/> is a snapshot at send time (same pattern as TaskComment) —
    /// history keeps the name the author had when the message was written.
    /// </summary>
    public static ChatMessage Create(Guid companyId, Guid conversationId, Guid authorUserId, string authorName, string? body, bool hasAttachments)
    {
        if (string.IsNullOrWhiteSpace(body) && !hasAttachments)
            throw new InvalidOperationException("A message needs text or at least one attachment.");

        return new ChatMessage(Guid.NewGuid(), companyId, conversationId, authorUserId, authorName, string.IsNullOrWhiteSpace(body) ? null : body.Trim());
    }

    public Guid CompanyId { get; private set; }
    public Guid ConversationId { get; private set; }
    public Guid AuthorUserId { get; private set; }

    /// <summary>Snapshot of the author's display name at send time.</summary>
    public string AuthorName { get; private set; } = string.Empty;

    public string? Body { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
