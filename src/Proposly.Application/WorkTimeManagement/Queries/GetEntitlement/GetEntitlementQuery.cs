using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;

namespace Proposly.Application.WorkTimeManagement.Queries.GetEntitlement;

/// <summary>
/// Vacation position for a year. Own by default; an approver may pass another employee's id.
/// </summary>
public sealed record GetEntitlementQuery(int Year, Guid? UserId = null)
    : IQuery<AbsenceEntitlementResponse?>;
