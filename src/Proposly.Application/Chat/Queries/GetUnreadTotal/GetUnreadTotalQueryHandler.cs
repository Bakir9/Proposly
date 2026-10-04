using Proposly.Application.Abstractions;
using Proposly.Application.Chat.Responses;
using Proposly.Application.Chat.Services;

namespace Proposly.Application.Chat.Queries.GetUnreadTotal;

public sealed class GetUnreadTotalQueryHandler(
    IChatReadService readService,
    ICurrentUserService currentUser) : IQueryHandler<GetUnreadTotalQuery, ChatUnreadResponse>
{
    public async Task<ChatUnreadResponse> HandleAsync(GetUnreadTotalQuery query, CancellationToken ct = default)
        => new(await readService.GetUnreadTotalAsync(currentUser.UserId, ct));
}
