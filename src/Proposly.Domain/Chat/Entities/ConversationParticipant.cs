using Proposly.Shared.Primitives;

namespace Proposly.Domain.Chat.Entities;

/// <summary>
/// Membership of one user in a conversation. Child of the <see cref="Conversation"/> aggregate —
/// it carries no CompanyId, so it must only ever be reached through Conversation.Participants
/// (a direct query would bypass the tenant filter).
/// </summary>
public sealed class ConversationParticipant : Entity<Guid>
{
    private ConversationParticipant() { } // For EF Core

    private ConversationParticipant(Guid id, Guid conversationId, Guid userId) : base(id)
    {
        ConversationId = conversationId;
        UserId = userId;
    }

    internal static ConversationParticipant Create(Guid conversationId, Guid userId)
        => new(Guid.NewGuid(), conversationId, userId);

    public Guid ConversationId { get; private set; }
    public Guid UserId { get; private set; }

    /// <summary>Everything up to and including this moment counts as read; unread counts are derived from it.</summary>
    public DateTime? LastReadAt { get; private set; }
    public Guid? LastReadMessageId { get; private set; }

    /// <summary>Manual "mark as unread": shows the conversation as unread even with no new messages.</summary>
    public bool IsMarkedUnread { get; private set; }

    internal void MarkRead(Guid? lastReadMessageId)
    {
        LastReadAt = DateTime.UtcNow;
        LastReadMessageId = lastReadMessageId;
        IsMarkedUnread = false;
    }

    internal void MarkUnread() => IsMarkedUnread = true;
}
