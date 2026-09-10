using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Shared.Primitives;

namespace Proposly.Domain.WorkTimeManagement.Events;

public sealed record AbsenceRejectedDomainEvent(
    Guid AbsenceId,
    Guid CompanyId,
    Guid UserId,
    AbsenceType Type,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid ApproverId,
    string Reason) : IDomainEvent;
