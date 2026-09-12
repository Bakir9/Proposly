using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;

namespace Proposly.Domain.WorkTimeManagement.Services;

/// <summary>
/// Turns a calendar range into chargeable working days and target hours.
/// <para>
/// A day is chargeable when it falls on one of the employee's working weekdays and is not a
/// non-working day that costs them nothing. That single rule is used for both absence counting
/// and target hours, which keeps the two consistent: a public holiday neither consumes vacation
/// nor adds to the target, while a company closure does both — the employee is expected to cover
/// it, and covers it with entitlement.
/// </para>
/// <para>
/// Every parameter for the pattern and calendar is optional. Omitting them falls back to Monday
/// to Friday with no holidays, which is what callers did before the calendar existed.
/// </para>
/// </summary>
public static class WorkingDayCalculator
{
    private const WeekDays DefaultPattern = WeekDays.MondayToFriday;

    /// <summary>
    /// Whether a date counts toward working days — in the pattern, and not a cost-free
    /// non-working day.
    /// </summary>
    public static bool IsChargeableDay(
        DateOnly date,
        WeekDays? pattern = null,
        IReadOnlyCollection<NonWorkingDay>? nonWorkingDays = null)
    {
        if (!(pattern ?? DefaultPattern).Includes(date)) return false;

        // Only days that cost the employee nothing drop out. A closure day they must cover with
        // entitlement stays chargeable.
        return nonWorkingDays is null
            || !nonWorkingDays.Any(d => d.Date == date && !d.ConsumesVacation);
    }

    public static int WorkingDaysInRange(
        DateOnly from,
        DateOnly to,
        WeekDays? pattern = null,
        IReadOnlyCollection<NonWorkingDay>? nonWorkingDays = null)
    {
        if (to < from) return 0;

        var count = 0;
        for (var date = from; date <= to; date = date.AddDays(1))
            if (IsChargeableDay(date, pattern, nonWorkingDays)) count++;

        return count;
    }

    /// <summary>
    /// Days an absence consumes, in half-day steps. A half-day marker only reduces the count when
    /// the day it falls on is itself chargeable.
    /// </summary>
    public static decimal ConsumedDays(
        DateOnly start,
        DateOnly end,
        bool firstDayIsHalf,
        bool lastDayIsHalf,
        WeekDays? pattern = null,
        IReadOnlyCollection<NonWorkingDay>? nonWorkingDays = null)
    {
        if (end < start)
            throw new ArgumentException("End date cannot precede start date.", nameof(end));

        var whole = WorkingDaysInRange(start, end, pattern, nonWorkingDays);
        if (whole == 0) return 0m;

        if (start == end)
            return firstDayIsHalf || lastDayIsHalf ? 0.5m : 1m;

        var consumed = (decimal)whole;

        if (firstDayIsHalf && IsChargeableDay(start, pattern, nonWorkingDays)) consumed -= 0.5m;
        if (lastDayIsHalf && IsChargeableDay(end, pattern, nonWorkingDays)) consumed -= 0.5m;

        return consumed;
    }

    public static IReadOnlyList<DateOnly> WorkingDatesInRange(
        DateOnly from,
        DateOnly to,
        WeekDays? pattern = null,
        IReadOnlyCollection<NonWorkingDay>? nonWorkingDays = null)
    {
        var dates = new List<DateOnly>();
        for (var date = from; date <= to; date = date.AddDays(1))
            if (IsChargeableDay(date, pattern, nonWorkingDays)) dates.Add(date);

        return dates;
    }

    /// <summary>The dates in a range that do not count, so a request can explain its own total.</summary>
    public static IReadOnlyList<DateOnly> NonWorkingDatesInRange(
        DateOnly from,
        DateOnly to,
        WeekDays? pattern = null,
        IReadOnlyCollection<NonWorkingDay>? nonWorkingDays = null)
    {
        var dates = new List<DateOnly>();
        for (var date = from; date <= to; date = date.AddDays(1))
            if (!IsChargeableDay(date, pattern, nonWorkingDays)) dates.Add(date);

        return dates;
    }

    /// <summary>Expected working days in a month, before absence is netted off.</summary>
    public static int ExpectedWorkingDays(
        int year,
        int month,
        IReadOnlyCollection<EmploymentTerms> termsVersions,
        IReadOnlyCollection<NonWorkingDay>? nonWorkingDays = null)
    {
        var (first, last) = MonthBounds(year, month);

        var count = 0;
        for (var date = first; date <= last; date = date.AddDays(1))
        {
            var terms = TermsOn(termsVersions, date);
            if (terms is null) continue;

            if (IsChargeableDay(date, terms.WorkingDays, nonWorkingDays)) count++;
        }

        return count;
    }

    /// <summary>
    /// Contracted hours the employee is expected to work in a month, with approved absence netted
    /// off so a week of leave does not read as a week of shortfall.
    /// <para>
    /// Returns null when no employment terms cover any day of the month — reported as unavailable
    /// rather than as zero, which would look like a full month of deficit.
    /// </para>
    /// <para>
    /// Terms are resolved per day rather than per month, so a contract change taking effect
    /// mid-month is handled correctly.
    /// </para>
    /// </summary>
    public static decimal? TargetHoursForMonth(
        int year,
        int month,
        IReadOnlyCollection<EmploymentTerms> termsVersions,
        IReadOnlyCollection<NonWorkingDay>? nonWorkingDays = null,
        IReadOnlyCollection<AbsenceRequest>? approvedAbsences = null)
    {
        var (first, last) = MonthBounds(year, month);

        var anyTermsApply = false;
        var target = 0m;

        for (var date = first; date <= last; date = date.AddDays(1))
        {
            var terms = TermsOn(termsVersions, date);
            if (terms is null) continue;

            anyTermsApply = true;

            if (!IsChargeableDay(date, terms.WorkingDays, nonWorkingDays)) continue;

            // Approved leave excuses the day, in whole or half.
            var excused = approvedAbsences is null
                ? 0m
                : Math.Min(1m, approvedAbsences
                    .Where(a => a.Status == AbsenceStatus.Approved)
                    .Sum(a => a.DayFraction(date)));

            target += terms.DailyHours * (1m - excused);
        }

        return anyTermsApply ? Math.Round(target, 2, MidpointRounding.AwayFromZero) : null;
    }

    /// <summary>The terms version in force on a date, or null when none covers it.</summary>
    public static EmploymentTerms? TermsOn(
        IReadOnlyCollection<EmploymentTerms> termsVersions, DateOnly date)
        => termsVersions
            .Where(t => t.AppliesOn(date))
            .OrderByDescending(t => t.ValidFrom)
            .FirstOrDefault();

    private static (DateOnly First, DateOnly Last) MonthBounds(int year, int month)
    {
        var first = new DateOnly(year, month, 1);
        return (first, first.AddMonths(1).AddDays(-1));
    }
}
