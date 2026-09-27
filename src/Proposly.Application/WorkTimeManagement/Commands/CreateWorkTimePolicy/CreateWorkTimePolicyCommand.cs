using Proposly.Application.Abstractions;

namespace Proposly.Application.WorkTimeManagement.Commands.CreateWorkTimePolicy;

public sealed record BreakRuleInput(decimal AboveHours, int MinBreakMinutes);

/// <summary>
/// Creates a new version of the company's working time policy, effective from
/// <paramref name="ValidFrom"/>. The predecessor is closed the day before; no already-approved
/// month is re-judged.
/// </summary>
public sealed record CreateWorkTimePolicyCommand(
    DateOnly ValidFrom,
    string Jurisdiction,
    string? HolidayRegionCode,
    decimal MaxHoursPerDay,
    decimal MaxHoursPerWeek,
    int AveragingWindowWeeks,
    decimal MaxAverageHoursPerWeek,
    decimal MinDailyRestHours,
    decimal MinWeeklyRestHours,
    decimal? SurplusCapHours,
    decimal? DeficitFloorHours,
    IReadOnlyList<BreakRuleInput> BreakRules) : ICommand<Guid>;
