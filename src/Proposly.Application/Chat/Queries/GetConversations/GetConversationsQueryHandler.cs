using Proposly.Application.Abstractions;
using Proposly.Application.Chat.Responses;
using Proposly.Application.Chat.Services;

namespace Proposly.Application.Chat.Queries.GetConversations;

public sealed class GetConversationsQueryHandler(
    IChatReadService readService,
    ICurrentUserService currentUser) : IQueryHandler<GetConversationsQuery, IReadOnlyList<ConversationResponse>>
{
    public async Task<IReadOnlyList<ConversationResponse>> HandleAsync(GetConversationsQuery query, CancellationToken ct = default)
        => await readService.GetConversationsForUserAsync(currentUser.UserId, query.Filter, ct);
}
