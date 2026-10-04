using Proposly.Application.Chat.Responses;

namespace Proposly.Application.Chat.Services;

/// <summary>
/// Read-only seam for the chat list/thread projections (same pattern as IProjectBookingReader).
/// Implementations compose unread counts, last-message previews and participant names in one
/// round trip instead of loading aggregates.
/// </summary>
public interface IChatReadService
{
    Task<IReadOnlyList<ConversationResponse>> GetConversationsForUserAsync(Guid userId, string? filter, CancellationToken ct = default);

    /// <summary>Null when the conversation does not exist or the user does not participate.</summary>
    Task<ConversationResponse?> GetConversationForUserAsync(Guid conversationId, Guid userId, CancellationToken ct = default);

    Task<ChatMessagesPageResponse> GetMessagesPageAsync(Guid conversationId, Guid? beforeMessageId, int limit, CancellationToken ct = default);

    Task<int> GetUnreadTotalAsync(Guid userId, CancellationToken ct = default);

    Task<IReadOnlyList<ConversationFileResponse>> GetConversationFilesAsync(Guid conversationId, CancellationToken ct = default);
}
