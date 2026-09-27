using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;

namespace Proposly.Application.WorkTimeManagement.Responses;

public sealed record BreakRuleResponse(decimal AboveHours, int MinBreakMinutes);

public sealed record PolicyVersionResponse(Guid Id, DateOnly ValidFrom, DateOnly? ValidTo);

/// <summary>
/// The company's working time agreement. A null response means no rule set is configured, which
/// the client must surface as a prompt rather than treating as "no limits".
/// </summary>
public sealed record WorkTimePolicyResponse(
    Guid? Id,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
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
    IReadOnlyList<BreakRuleResponse> BreakRules,
    IReadOnlyList<PolicyVersionResponse> History);

/// <summary>One detected breach, as shown on a timesheet or a report.</summary>
public sealed record BreachResponse(
    BreachKind Kind,
    DateOnly? Date,
    DateOnly? WeekStartDate,
    decimal LimitValue,
    decimal ActualValue,
    DateTime? AcknowledgedAt);

/// <summary>A breach with its employee, for the company-wide compliance overview.</summary>
public sealed record BreachOverviewResponse(
    Guid UserId,
    string EmployeeName,
    int Year,
    int Month,
    BreachKind Kind,
    DateOnly? Date,
    DateOnly? WeekStartDate,
    decimal LimitValue,
    decimal ActualValue,
    string TimesheetStatus);

public static class WorkTimePolicyMappingExtensions
{
    public static WorkTimePolicyResponse ToResponse(
        this WorkTimePolicy policy, IReadOnlyList<PolicyVersionResponse>? history = null)
        => new(
            policy.Id == Guid.Empty ? null : policy.Id,
            policy.ValidFrom,
            policy.ValidTo,
            policy.Jurisdiction,
            policy.HolidayRegionCode,
            policy.MaxHoursPerDay,
            policy.MaxHoursPerWeek,
            policy.AveragingWindowWeeks,
            policy.MaxAverageHoursPerWeek,
            policy.MinDailyRestHours,
            policy.MinWeeklyRestHours,
            policy.SurplusCapHours,
            policy.DeficitFloorHours,
            policy.BreakRules
                .OrderBy(r => r.AboveHours)
                .Select(r => new BreakRuleResponse(r.AboveHours, r.MinBreakMinutes))
                .ToList(),
            history ?? []);

    public static BreachResponse ToResponse(this TimesheetBreach breach)
        => new(
            breach.Kind,
            breach.Date,
            breach.WeekStartDate,
            breach.LimitValue,
            breach.ActualValue,
            breach.AcknowledgedAt);
}
