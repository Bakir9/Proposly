using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Shared.Primitives;

namespace Proposly.Domain.WorkTimeManagement.Events;

public sealed record AbsenceRequestedDomainEvent(
    Guid AbsenceId,
    Guid CompanyId,
    Guid UserId,
    AbsenceType Type,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal ConsumedDays) : IDomainEvent;
