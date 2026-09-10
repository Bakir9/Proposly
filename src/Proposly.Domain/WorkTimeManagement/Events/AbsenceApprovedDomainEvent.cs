using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Shared.Primitives;

namespace Proposly.Domain.WorkTimeManagement.Events;

public sealed record AbsenceApprovedDomainEvent(
    Guid AbsenceId,
    Guid CompanyId,
    Guid UserId,
    AbsenceType Type,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid ApproverId) : IDomainEvent;
