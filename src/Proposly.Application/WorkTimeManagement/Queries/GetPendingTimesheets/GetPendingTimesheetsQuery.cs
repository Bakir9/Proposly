using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;

namespace Proposly.Application.WorkTimeManagement.Queries.GetPendingTimesheets;

/// <summary>Submitted months awaiting an approver decision, across the company.</summary>
public sealed record GetPendingTimesheetsQuery
    : IQuery<IReadOnlyList<TimesheetSummaryResponse>>;
