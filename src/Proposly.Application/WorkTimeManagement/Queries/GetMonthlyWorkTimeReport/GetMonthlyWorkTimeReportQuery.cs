using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;

namespace Proposly.Application.WorkTimeManagement.Queries.GetMonthlyWorkTimeReport;

/// <summary>
/// One employee's month end. Own by default; an approver may pass another employee's id — and a
/// member passing someone else's gets nothing, because the query filter hides the row.
/// </summary>
public sealed record GetMonthlyWorkTimeReportQuery(int Year, int Month, Guid? UserId = null)
    : IQuery<MonthlyWorkTimeReportResponse?>;
