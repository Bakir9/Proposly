using Proposly.Application.Abstractions;
using Proposly.Application.CalendarManagement.Responses;
using Proposly.Domain.CalendarManagement.Repositories;

namespace Proposly.Application.CalendarManagement.Queries.GetPendingInvitations;

public sealed class GetPendingInvitationsQueryHandler
    : IQueryHandler<GetPendingInvitationsQuery, IReadOnlyList<TerminSummaryResponse>>
{
    private readonly ITerminRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public GetPendingInvitationsQueryHandler(ITerminRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<TerminSummaryResponse>> HandleAsync(
        GetPendingInvitationsQuery query,
        CancellationToken cancellationToken = default)
    {
        var termins = await _repository.GetPendingInvitationsAsync(_currentUser.UserId, cancellationToken);
        return termins.Select(t => t.ToSummary(_currentUser.UserId)).ToList();
    }
}
