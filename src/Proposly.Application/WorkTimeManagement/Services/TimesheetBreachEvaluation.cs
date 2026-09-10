using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Services;

namespace Proposly.Application.WorkTimeManagement.Services;

/// <summary>
/// Resolves the policy and the history a compliance check needs, then runs the evaluator.
/// <para>
/// Shared by the read path and the approval path so both judge a month identically.
/// </para>
/// </summary>
internal static class TimesheetBreachEvaluation
{
    /// <summary>
    /// Breaches for a month. An open month is evaluated live, so correcting a day clears its flag
    /// at once; an approved or locked month returns the set frozen at approval, which a later
    /// policy change must not rewrite.
    /// </summary>
    public static async Task<IReadOnlyList<TimesheetBreach>> ResolveAsync(
        Timesheet timesheet,
        IWorkTimePolicyRepository policies,
        ITimesheetRepository timesheets,
        IAbsenceRepository absences,
        CancellationToken ct = default)
    {
        if (timesheet.Status is TimesheetStatus.Approved or TimesheetStatus.Locked)
            return [.. timesheet.Breaches];

        return await EvaluateAsync(timesheet, policies, timesheets, absences, ct);
    }

    /// <summary>
    /// Evaluates the month from scratch. Returns empty when the company has no policy configured —
    /// the caller surfaces that as "no rule set active" rather than as compliance.
    /// </summary>
    public static async Task<IReadOnlyList<TimesheetBreach>> EvaluateAsync(
        Timesheet timesheet,
        IWorkTimePolicyRepository policies,
        ITimesheetRepository timesheets,
        IAbsenceRepository absences,
        CancellationToken ct = default)
    {
        var monthStart = new DateOnly(timesheet.Year, timesheet.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var policy = await policies.GetEffectiveAsync(monthStart, ct);
        if (policy is null) return [];

        // Reaches back over the averaging window, which also covers the single preceding day the
        // cross-boundary rest check needs.
        var windowStart = monthStart.AddDays(-(policy.AveragingWindowWeeks * 7));

        var preceding = await timesheets.GetDaysInRangeAsync(
            timesheet.UserId, windowStart, monthStart.AddDays(-1), ct);

        // Spans the same window, since a preceding day covered by leave must be excluded too.
        var approvedAbsences = await absences.GetApprovedInRangeAsync(
            timesheet.UserId, windowStart, monthEnd, ct);

        return ComplianceEvaluator.Evaluate(
            timesheet.Id, timesheet.Days, policy, preceding, approvedAbsences);
    }
}
