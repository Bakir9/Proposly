using Proposly.Application.Abstractions;
using Proposly.Application.Chat.Responses;
using Proposly.Application.Chat.Services;
using Proposly.Domain.Chat.Repositories;
using Proposly.Shared.Exceptions;

namespace Proposly.Application.Chat.Queries.GetMessages;

public sealed class GetMessagesQueryHandler(
    IChatReadService readService,
    IConversationRepository conversations,
    ICurrentUserService currentUser) : IQueryHandler<GetMessagesQuery, ChatMessagesPageResponse>
{
    public async Task<ChatMessagesPageResponse> HandleAsync(GetMessagesQuery query, CancellationToken ct = default)
    {
        var conversation = await conversations.GetByIdAsync(query.ConversationId, ct)
            ?? throw AppException.NotFound("conversation.not_found", $"Conversation {query.ConversationId} not found.");

        if (!conversation.IsParticipant(currentUser.UserId))
            throw new InvalidOperationException("Only participants can read this conversation.");

        var limit = Math.Clamp(query.Limit, 1, 200);
        return await readService.GetMessagesPageAsync(conversation.Id, query.Before, limit, ct);
    }
}
