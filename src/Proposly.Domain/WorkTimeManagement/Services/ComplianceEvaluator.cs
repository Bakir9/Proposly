using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;

namespace Proposly.Domain.WorkTimeManagement.Services;

/// <summary>
/// Checks recorded working time against a company's <see cref="WorkTimePolicy"/>.
/// <para>
/// Pure and side-effect free, following the VatCalculator precedent. It reports; it never blocks.
/// Callers must not use its output to refuse a day entry — recording the truth takes precedence
/// over recording something compliant (FR-051).
/// </para>
/// </summary>
public static class ComplianceEvaluator
{
    /// <param name="days">The days of the month being evaluated.</param>
    /// <param name="precedingDays">
    /// Days before the month, needed for two checks that reach backwards: the rest period between
    /// the last day of the previous month and the first of this one, and the rolling average.
    /// </param>
    /// <param name="approvedAbsences">
    /// Approved absences for the employee. A day covered by one contributes nothing to any limit:
    /// someone on approved leave who logged a few hours has not breached a working time rule.
    /// </param>
    public static IReadOnlyList<TimesheetBreach> Evaluate(
        Guid timesheetId,
        IReadOnlyCollection<WorkDayEntry> days,
        WorkTimePolicy policy,
        IReadOnlyCollection<WorkDayEntry>? precedingDays = null,
        IReadOnlyCollection<AbsenceRequest>? approvedAbsences = null)
    {
        var breaches = new List<TimesheetBreach>();
        if (days.Count == 0) return breaches;

        var monthDays = Excluding(days, approvedAbsences);
        if (monthDays.Count == 0) return breaches;

        precedingDays = Excluding(precedingDays, approvedAbsences);

        breaches.AddRange(EvaluateDailyMaximum(timesheetId, monthDays, policy));
        breaches.AddRange(EvaluateBreaks(timesheetId, monthDays, policy));
        breaches.AddRange(EvaluateDailyRest(timesheetId, monthDays, policy, precedingDays));
        breaches.AddRange(EvaluateWeeklyMaximum(timesheetId, monthDays, policy, precedingDays));
        breaches.AddRange(EvaluateAveragingWindow(timesheetId, monthDays, policy, precedingDays));

        return breaches;
    }

    private static IEnumerable<TimesheetBreach> EvaluateDailyMaximum(
        Guid timesheetId, List<WorkDayEntry> days, WorkTimePolicy policy)
        => days
            .Where(d => d.WorkedHours > policy.MaxHoursPerDay)
            .Select(d => TimesheetBreach.ForDay(
                timesheetId, BreachKind.DailyMaximum, d.Date, policy.MaxHoursPerDay, d.WorkedHours));

    private static IEnumerable<TimesheetBreach> EvaluateBreaks(
        Guid timesheetId, List<WorkDayEntry> days, WorkTimePolicy policy)
    {
        foreach (var day in days)
        {
            var required = policy.RequiredBreakMinutes(day.WorkedHours);
            if (required > 0 && day.BreakMinutes < required)
            {
                yield return TimesheetBreach.ForDay(
                    timesheetId, BreachKind.InsufficientBreak, day.Date, required, day.BreakMinutes);
            }
        }
    }

    private static IEnumerable<TimesheetBreach> EvaluateDailyRest(
        Guid timesheetId,
        List<WorkDayEntry> days,
        WorkTimePolicy policy,
        IReadOnlyCollection<WorkDayEntry>? precedingDays)
    {
        if (policy.MinDailyRestHours <= 0) yield break;

        // The day immediately before the month lets a rest breach spanning the boundary be found.
        var previous = precedingDays?
            .Where(d => d.Date < days[0].Date)
            .OrderByDescending(d => d.Date)
            .FirstOrDefault();

        var chain = previous is null ? days : [previous, .. days];

        for (var i = 1; i < chain.Count; i++)
        {
            var earlier = chain[i - 1];
            var later = chain[i];

            var restHours = (decimal)(StartOf(later) - EndOf(earlier)).TotalHours;

            // Negative rest means the shifts overlap, which is itself a breach of the rule.
            if (restHours < policy.MinDailyRestHours)
            {
                yield return TimesheetBreach.ForDay(
                    timesheetId, BreachKind.InsufficientDailyRest, later.Date,
                    policy.MinDailyRestHours, Math.Round(restHours, 2, MidpointRounding.AwayFromZero));
            }
        }
    }

    private static IEnumerable<TimesheetBreach> EvaluateWeeklyMaximum(
        Guid timesheetId,
        List<WorkDayEntry> days,
        WorkTimePolicy policy,
        IReadOnlyCollection<WorkDayEntry>? precedingDays)
    {
        // Weeks are keyed by their Monday so a week straddling a month boundary is counted whole.
        var all = precedingDays is null ? days : [.. precedingDays, .. days];
        var monthWeeks = days.Select(d => WeekStart(d.Date)).Distinct().ToHashSet();

        foreach (var weekStart in monthWeeks.OrderBy(w => w))
        {
            var weekEnd = weekStart.AddDays(6);
            var total = all
                .Where(d => d.Date >= weekStart && d.Date <= weekEnd)
                .Sum(d => d.WorkedHours);

            if (total > policy.MaxHoursPerWeek)
            {
                yield return TimesheetBreach.ForWeek(
                    timesheetId, BreachKind.WeeklyMaximum, weekStart, policy.MaxHoursPerWeek, total);
            }
        }
    }

    private static IEnumerable<TimesheetBreach> EvaluateAveragingWindow(
        Guid timesheetId,
        List<WorkDayEntry> days,
        WorkTimePolicy policy,
        IReadOnlyCollection<WorkDayEntry>? precedingDays)
    {
        if (policy.MaxAverageHoursPerWeek <= 0) yield break;

        var all = precedingDays is null ? days : [.. precedingDays, .. days];
        var windowDays = policy.AveragingWindowWeeks * 7;

        // Evaluated at each week-end inside the month, looking back over the window.
        foreach (var weekStart in days.Select(d => WeekStart(d.Date)).Distinct().OrderBy(w => w))
        {
            var windowEnd = weekStart.AddDays(6);
            var windowStart = windowEnd.AddDays(-(windowDays - 1));

            var total = all
                .Where(d => d.Date >= windowStart && d.Date <= windowEnd)
                .Sum(d => d.WorkedHours);

            var average = total / policy.AveragingWindowWeeks;

            if (average > policy.MaxAverageHoursPerWeek)
            {
                yield return TimesheetBreach.ForWeek(
                    timesheetId, BreachKind.AveragingWindow, weekStart,
                    policy.MaxAverageHoursPerWeek,
                    Math.Round(average, 2, MidpointRounding.AwayFromZero));
            }
        }
    }

    /// <summary>
    /// Drops days covered by an approved absence, ordered by date. Approved leave excuses the day
    /// entirely rather than reducing it (FR-057).
    /// </summary>
    private static List<WorkDayEntry> Excluding(
        IReadOnlyCollection<WorkDayEntry>? days,
        IReadOnlyCollection<AbsenceRequest>? approvedAbsences)
    {
        if (days is null || days.Count == 0) return [];

        var ordered = days.OrderBy(d => d.Date);

        if (approvedAbsences is null || approvedAbsences.Count == 0)
            return [.. ordered];

        // Approved only. A request still awaiting a decision excuses nothing — the status is
        // re-checked here rather than trusting the caller to have filtered.
        return [.. ordered.Where(d => !approvedAbsences.Any(
            a => a.Status == AbsenceStatus.Approved && a.CoversDate(d.Date)))];
    }

    private static DateTime StartOf(WorkDayEntry day)
        => day.Date.ToDateTime(day.StartTime);

    private static DateTime EndOf(WorkDayEntry day)
    {
        var end = day.Date.ToDateTime(day.EndTime);
        return day.CrossesMidnight ? end.AddDays(1) : end;
    }

    /// <summary>The Monday of the week containing <paramref name="date"/>.</summary>
    private static DateOnly WeekStart(DateOnly date)
    {
        var offset = ((int)date.DayOfWeek + 6) % 7; // Monday == 0
        return date.AddDays(-offset);
    }
}
