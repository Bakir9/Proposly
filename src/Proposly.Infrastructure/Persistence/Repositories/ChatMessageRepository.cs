using Microsoft.EntityFrameworkCore;
using Proposly.Domain.Chat.Entities;
using Proposly.Domain.Chat.Repositories;

namespace Proposly.Infrastructure.Persistence.Repositories;

public sealed class ChatMessageRepository(AppDbContext context) : IChatMessageRepository
{
    public async Task AddAsync(ChatMessage message, CancellationToken ct = default)
        => await context.ChatMessages.AddAsync(message, ct);

    public async Task AddAttachmentAsync(ChatAttachment attachment, CancellationToken ct = default)
        => await context.ChatAttachments.AddAsync(attachment, ct);

    public async Task<ChatAttachment?> GetAttachmentByIdAsync(Guid id, CancellationToken ct = default)
        => await context.ChatAttachments.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<ChatAttachment>> GetPendingAttachmentsAsync(
        IReadOnlyCollection<Guid> ids, Guid conversationId, CancellationToken ct = default)
    {
        if (ids.Count == 0)
            return [];

        return await context.ChatAttachments
            .Where(a => ids.Contains(a.Id) && a.ConversationId == conversationId && a.MessageId == null)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ChatAttachment>> GetAttachmentsForMessageAsync(Guid messageId, CancellationToken ct = default)
        => await context.ChatAttachments
            .Where(a => a.MessageId == messageId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => context.SaveChangesAsync(ct);
}
