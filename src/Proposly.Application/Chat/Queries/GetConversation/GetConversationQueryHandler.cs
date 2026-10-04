using Proposly.Application.Abstractions;
using Proposly.Application.Chat.Responses;
using Proposly.Application.Chat.Services;

namespace Proposly.Application.Chat.Queries.GetConversation;

public sealed class GetConversationQueryHandler(
    IChatReadService readService,
    ICurrentUserService currentUser) : IQueryHandler<GetConversationQuery, ConversationResponse?>
{
    public async Task<ConversationResponse?> HandleAsync(GetConversationQuery query, CancellationToken ct = default)
        => await readService.GetConversationForUserAsync(query.ConversationId, currentUser.UserId, ct);
}
