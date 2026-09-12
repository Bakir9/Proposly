using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;

namespace Proposly.Application.WorkTimeManagement.Queries.GetNonWorkingDays;

/// <summary>
/// The company holiday and closure calendar for a range. Readable by every employee — it shapes
/// their own target hours and absence counts.
/// </summary>
public sealed record GetNonWorkingDaysQuery(DateOnly From, DateOnly To)
    : IQuery<IReadOnlyList<NonWorkingDayResponse>>;
