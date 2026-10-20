using Proposly.Application.Abstractions;
using Proposly.Domain.WorkTimeManagement.Enums;

namespace Proposly.Application.Hr.Commands.AddWorkerAbsence;

/// <summary>
/// Records an absence on behalf of a worker (HR view). Sick leave is approved immediately;
/// everything else starts Pending and goes through the normal approval endpoint.
/// </summary>
public sealed record AddWorkerAbsenceCommand(
    Guid UserId,
    AbsenceType Type,
    DateOnly StartDate,
    DateOnly EndDate) : ICommand<Guid>;
