using Proposly.Application.Abstractions;
using Proposly.Application.CalendarManagement.Responses;
using Proposly.Domain.CalendarManagement.Repositories;

namespace Proposly.Application.CalendarManagement.Queries.GetTerminById;

public sealed class GetTerminByIdQueryHandler : IQueryHandler<GetTerminByIdQuery, TerminDetailResponse?>
{
    private readonly ITerminRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public GetTerminByIdQueryHandler(ITerminRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<TerminDetailResponse?> HandleAsync(GetTerminByIdQuery query, CancellationToken cancellationToken = default)
    {
        var termin = await _repository.GetByIdAsync(query.Id, cancellationToken);
        return termin?.ToDetail(_currentUser.UserId);
    }
}
