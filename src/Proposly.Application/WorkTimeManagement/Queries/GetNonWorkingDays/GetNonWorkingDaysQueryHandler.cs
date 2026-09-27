using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Queries.GetNonWorkingDays;

public sealed class GetNonWorkingDaysQueryHandler
    : IQueryHandler<GetNonWorkingDaysQuery, IReadOnlyList<NonWorkingDayResponse>>
{
    private readonly INonWorkingDayRepository _calendar;

    public GetNonWorkingDaysQueryHandler(INonWorkingDayRepository calendar) => _calendar = calendar;

    public async Task<IReadOnlyList<NonWorkingDayResponse>> HandleAsync(
        GetNonWorkingDaysQuery query, CancellationToken ct = default)
    {
        var days = await _calendar.GetForRangeAsync(query.From, query.To, ct);

        return days.Select(d => d.ToResponse()).ToList();
    }
}
