using Proposly.Application.Abstractions;
using Proposly.Application.CalendarManagement.Responses;
using Proposly.Domain.CalendarManagement.Repositories;

namespace Proposly.Application.CalendarManagement.Queries.GetUserAvailability;

public sealed class GetUserAvailabilityQueryHandler : IQueryHandler<GetUserAvailabilityQuery, UserAvailabilityResponse>
{
    private readonly ITerminRepository _repository;

    public GetUserAvailabilityQueryHandler(ITerminRepository repository) => _repository = repository;

    public async Task<UserAvailabilityResponse> HandleAsync(GetUserAvailabilityQuery query, CancellationToken cancellationToken = default)
    {
        var termins = await _repository.GetAvailabilityAsync(query.UserId, query.Start, query.End, cancellationToken);
        var summaries = termins.Select(t => t.ToSummary(query.UserId)).ToList();
        return new UserAvailabilityResponse(query.UserId, summaries);
    }
}
