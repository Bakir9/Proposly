using Proposly.Domain.Chat.Entities;

namespace Proposly.Domain.Chat.Repositories;

public interface IConversationRepository
{
    /// <summary>Loads the conversation with its participants.</summary>
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Conversation?> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default);

    /// <summary>The existing direct conversation between the two users, if any.</summary>
    Task<Conversation?> GetDirectBetweenAsync(Guid userA, Guid userB, CancellationToken ct = default);

    Task AddAsync(Conversation conversation, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
