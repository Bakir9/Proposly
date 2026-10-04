using Microsoft.EntityFrameworkCore;
using Proposly.Domain.Chat.Entities;
using Proposly.Domain.Chat.Enums;
using Proposly.Domain.Chat.Repositories;

namespace Proposly.Infrastructure.Persistence.Repositories;

public sealed class ConversationRepository(AppDbContext context) : IConversationRepository
{
    public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Conversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Conversation?> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default)
        => await context.Conversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.ProjectId == projectId, ct);

    public async Task<Conversation?> GetDirectBetweenAsync(Guid userA, Guid userB, CancellationToken ct = default)
        => await context.Conversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c =>
                c.Kind == ConversationKind.Direct
                && c.Participants.Any(p => p.UserId == userA)
                && c.Participants.Any(p => p.UserId == userB), ct);

    public async Task AddAsync(Conversation conversation, CancellationToken ct = default)
        => await context.Conversations.AddAsync(conversation, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => context.SaveChangesAsync(ct);
}
