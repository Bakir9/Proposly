using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;

namespace Proposly.Application.WorkTimeManagement.Queries.GetReconciliation;

/// <summary>
/// Recorded working hours against hours booked to projects, for one employee and month.
/// Informational only — it never changes either record.
/// </summary>
public sealed record GetReconciliationQuery(int Year, int Month, Guid? UserId = null)
    : IQuery<ReconciliationResponse?>;
