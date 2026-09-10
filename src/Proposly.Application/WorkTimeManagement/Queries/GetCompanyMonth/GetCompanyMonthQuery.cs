using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;

namespace Proposly.Application.WorkTimeManagement.Queries.GetCompanyMonth;

/// <summary>Every employee's month in one call, for the owner and admin overview.</summary>
public sealed record GetCompanyMonthQuery(int Year, int Month)
    : IQuery<IReadOnlyList<TimesheetSummaryResponse>>;
