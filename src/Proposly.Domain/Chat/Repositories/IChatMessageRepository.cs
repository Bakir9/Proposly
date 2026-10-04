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

    Task SaveChangesAsync(CancellationToken ct = default);
}
