using Proposly.Shared.Primitives;

namespace Proposly.Domain.WorkTimeManagement.Events;

public sealed record TimesheetSubmittedDomainEvent(
    Guid TimesheetId,
    Guid CompanyId,
    Guid UserId,
    int Year,
    int Month) : IDomainEvent;
