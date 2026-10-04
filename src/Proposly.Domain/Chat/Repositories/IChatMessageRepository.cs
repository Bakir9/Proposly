using Proposly.Domain.Chat.Entities;

namespace Proposly.Domain.Chat.Repositories;

public interface IChatMessageRepository
{
    Task AddAsync(ChatMessage message, CancellationToken ct = default);

    Task AddAttachmentAsync(ChatAttachment attachment, CancellationToken ct = default);
    Task<ChatAttachment?> GetAttachmentByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>The given attachments while they are still pending (not yet bound to a message).</summary>
    Task<IReadOnlyList<ChatAttachment>> GetPendingAttachmentsAsync(IReadOnlyCollection<Guid> ids, Guid conversationId, CancellationToken ct = default);

    /// <summary>Attachments of one message, for building the send response.</summary>
    Task<IReadOnlyList<ChatAttachment>> GetAttachmentsForMessageAsync(Guid messageId, CancellationToken ct = default);

    /// <summary>Storage keys of every attachment in a conversation (sent and pending) — for blob cleanup on delete.</summary>
    Task<IReadOnlyList<string>> GetAttachmentStorageKeysAsync(Guid conversationId, CancellationToken ct = default);

    /// <summary>Removes every attachment row of a conversation, including pending ones the cascade cannot reach.</summary>
    Task RemoveAttachmentsForConversationAsync(Guid conversationId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
