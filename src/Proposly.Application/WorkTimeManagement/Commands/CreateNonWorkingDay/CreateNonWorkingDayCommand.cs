using Proposly.Application.Abstractions;
using Proposly.Domain.WorkTimeManagement.Enums;

namespace Proposly.Application.WorkTimeManagement.Commands.CreateNonWorkingDay;

/// <summary>
/// Adds a holiday or company closure day. <paramref name="ConsumesVacation"/> defaults by kind
/// when omitted: false for a public holiday, true for a closure.
/// </summary>
public sealed record CreateNonWorkingDayCommand(
    DateOnly Date,
    string Name,
    NonWorkingDayKind Kind,
    bool? ConsumesVacation = null) : ICommand<Guid>;
