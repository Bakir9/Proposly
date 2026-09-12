using Proposly.Application.Abstractions;
using Proposly.Domain.WorkTimeManagement.Enums;

namespace Proposly.Application.WorkTimeManagement.Commands.UpdateNonWorkingDay;

/// <summary>
/// Corrects a non-working day. The date is immutable — delete and recreate to move one, so the
/// one-entry-per-date rule stays enforceable.
/// </summary>
public sealed record UpdateNonWorkingDayCommand(
    Guid Id,
    string Name,
    NonWorkingDayKind Kind,
    bool ConsumesVacation) : ICommand;
