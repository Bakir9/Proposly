using Proposly.Domain.Chat.Enums;
using Proposly.Shared.Interfaces;
using Proposly.Shared.Primitives;

namespace Proposly.Domain.Chat.Entities;

public sealed class Conversation : AggregateRoot<Guid>, ITenantEntity, IAuditableEntity
{
    private readonly List<ConversationParticipant> _participants = [];

    private Conversation() { } // For EF Core

    private Conversation(Guid id, Guid companyId, ConversationKind kind, string? title, Guid? projectId)
        : base(id)
    {
        CompanyId = companyId;
        Kind = kind;
        Title = title;
        ProjectId = projectId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Conversation CreateDirect(Guid companyId, Guid userA, Guid userB)
    {
        if (userA == userB)
            throw new InvalidOperationException("A direct conversation needs two different participants.");

        var conversation = new Conversation(Guid.NewGuid(), companyId, ConversationKind.Direct, title: null, projectId: null);
        conversation._participants.Add(ConversationParticipant.Create(conversation.Id, userA));
        conversation._participants.Add(ConversationParticipant.Create(conversation.Id, userB));
        return conversation;
    }

    public static Conversation CreateGroup(Guid companyId, string title, IReadOnlyCollection<Guid> participantUserIds)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new InvalidOperationException("A group conversation needs a title.");

        var distinct = participantUserIds.Distinct().ToList();
        if (distinct.Count < 2)
            throw new InvalidOperationException("A group conversation needs at least 2 participants.");

        var conversation = new Conversation(Guid.NewGuid(), companyId, ConversationKind.Group, title.Trim(), projectId: null);
        foreach (var userId in distinct)
            conversation._participants.Add(ConversationParticipant.Create(conversation.Id, userId));
        return conversation;
    }

    /// <summary>
    /// The project's channel. The title is a snapshot of the project name at creation time;
    /// uniqueness per project is enforced by the database index on ProjectId.
    /// </summary>
    public static Conversation CreateProjectChannel(Guid companyId, Guid projectId, string projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName))
            throw new InvalidOperationException("A project channel needs the project name as its title.");

        return new Conversation(Guid.NewGuid(), companyId, ConversationKind.Project, projectName.Trim(), projectId);
    }

    public Guid CompanyId { get; private set; }
    public ConversationKind Kind { get; private set; }
    public string? Title { get; private set; }
    public Guid? ProjectId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>Set on every sent message so conversation lists can sort without scanning messages.</summary>
    public DateTime? LastMessageAt { get; private set; }

    public IReadOnlyCollection<ConversationParticipant> Participants => _participants.AsReadOnly();

    public bool IsParticipant(Guid userId) => _participants.Any(p => p.UserId == userId);

    public ConversationParticipant AddParticipant(Guid userId)
    {
        if (Kind == ConversationKind.Direct)
            throw new InvalidOperationException("A direct conversation always has exactly 2 participants.");

        var existing = _participants.FirstOrDefault(p => p.UserId == userId);
        if (existing is not null)
            return existing;

        var participant = ConversationParticipant.Create(Id, userId);
        _participants.Add(participant);
        Touch();
        return participant;
    }

    public void RemoveParticipant(Guid userId)
    {
        if (Kind == ConversationKind.Direct)
            throw new InvalidOperationException("A direct conversation always has exactly 2 participants.");

        var participant = _participants.FirstOrDefault(p => p.UserId == userId);
        if (participant is null)
            return;

        _participants.Remove(participant);
        Touch();
    }

    /// <summary>
    /// Makes sure every given user participates. Add-only and idempotent — leaving a channel is a
    /// deliberate act (RemoveParticipant), never a side effect of a sync.
    /// </summary>
    public void EnsureParticipants(IReadOnlyCollection<Guid> userIds)
    {
        if (Kind == ConversationKind.Direct)
            throw new InvalidOperationException("A direct conversation always has exactly 2 participants.");

        var added = false;
        foreach (var userId in userIds.Distinct().Where(id => _participants.All(p => p.UserId != id)))
        {
            _participants.Add(ConversationParticipant.Create(Id, userId));
            added = true;
        }

        if (added)
            Touch();
    }

    public void MarkRead(Guid userId, Guid? lastReadMessageId)
    {
        var participant = _participants.FirstOrDefault(p => p.UserId == userId)
            ?? throw new InvalidOperationException("Only participants can mark a conversation as read.");
        participant.MarkRead(lastReadMessageId);
    }

    public void MarkUnread(Guid userId)
    {
        var participant = _participants.FirstOrDefault(p => p.UserId == userId)
            ?? throw new InvalidOperationException("Only participants can mark a conversation as unread.");
        participant.MarkUnread();
    }

    /// <summary>
    /// Deleting is reserved for direct and group chats. A project channel belongs to its
    /// project and disappears only with it.
    /// </summary>
    public void EnsureDeletableBy(Guid userId)
    {
        if (Kind == ConversationKind.Project)
            throw new InvalidOperationException("A project channel cannot be deleted — it belongs to the project.");
        if (!IsParticipant(userId))
            throw new InvalidOperationException("Only participants can delete this conversation.");
    }

    public void RegisterMessageSent(DateTime sentAt)
    {
        LastMessageAt = sentAt;
        Touch();
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
