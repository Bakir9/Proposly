using Proposly.Shared.Primitives;

namespace Proposly.Domain.WorkTimeManagement.Events;

public sealed record TimesheetApprovedDomainEvent(
    Guid TimesheetId,
    Guid CompanyId,
    Guid UserId,
    int Year,
    int Month,
    Guid ApproverId) : IDomainEvent;
