using Proposly.Application.Abstractions;
using Proposly.Application.CalendarManagement.Responses;
using Proposly.Domain.CalendarManagement.Enums;
using Proposly.Domain.CalendarManagement.Repositories;

namespace Proposly.Application.CalendarManagement.Queries.GetTermins;

public sealed class GetTerminsQueryHandler : IQueryHandler<GetTerminsQuery, IReadOnlyList<TerminSummaryResponse>>
{
    private readonly ITerminRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public GetTerminsQueryHandler(ITerminRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<TerminSummaryResponse>> HandleAsync(GetTerminsQuery query, CancellationToken cancellationToken = default)
    {
        var termins = await _repository.GetForUserAsync(_currentUser.UserId, query.Start, query.End, cancellationToken);
        return termins.Select(t => t.ToSummary(_currentUser.UserId)).ToList();
    }
}
