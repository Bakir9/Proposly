using Proposly.Application.Abstractions;
using Proposly.Domain.WorkTimeManagement.Enums;

namespace Proposly.Application.WorkTimeManagement.Commands.RequestAbsence;

/// <summary>
/// Requests time off for the caller. The days consumed are calculated server-side, never taken
/// from the client.
/// <para>
/// <paramref name="Reason"/> is optional, non-medical free text. Sick leave needs nothing beyond
/// the type and dates.
/// </para>
/// </summary>
public sealed record RequestAbsenceCommand(
    AbsenceType Type,
    DateOnly StartDate,
    DateOnly EndDate,
    bool FirstDayIsHalf = false,
    bool LastDayIsHalf = false,
    string? Reason = null) : ICommand<Guid>;
