namespace Proposly.Domain.WorkTimeManagement.Services;

/// <summary>
/// Counts the working days an absence consumes.
/// <para>
/// Currently excludes Saturdays and Sundays only. Employment terms (which weekdays a person
/// actually works) and the company holiday calendar arrive in the next increment, at which point
/// this gains a working-day pattern and a non-working-day list; the call sites stay the same and
/// simply pass more.
/// </para>
/// </summary>
public static class WorkingDayCalculator
{
    /// <summary>
    /// Working days between two dates inclusive, as a whole number.
    /// </summary>
    public static int WorkingDaysInRange(DateOnly from, DateOnly to)
    {
        if (to < from) return 0;

        var count = 0;
        for (var date = from; date <= to; date = date.AddDays(1))
            if (IsWorkingDay(date)) count++;

        return count;
    }

    /// <summary>
    /// Days an absence consumes, in half-day steps. A half-day marker only reduces the count when
    /// the day it falls on is itself a working day.
    /// </summary>
    public static decimal ConsumedDays(
        DateOnly start, DateOnly end, bool firstDayIsHalf, bool lastDayIsHalf)
    {
        if (end < start)
            throw new ArgumentException("End date cannot precede start date.", nameof(end));

        var whole = WorkingDaysInRange(start, end);
        if (whole == 0) return 0m;

        // A single day marked half either way is half a day, not none.
        if (start == end)
            return (firstDayIsHalf || lastDayIsHalf) ? 0.5m : 1m;

        var consumed = (decimal)whole;

        if (firstDayIsHalf && IsWorkingDay(start)) consumed -= 0.5m;
        if (lastDayIsHalf && IsWorkingDay(end)) consumed -= 0.5m;

        return consumed;
    }

    /// <summary>The dates in a range that count as working days.</summary>
    public static IReadOnlyList<DateOnly> WorkingDatesInRange(DateOnly from, DateOnly to)
    {
        var dates = new List<DateOnly>();
        for (var date = from; date <= to; date = date.AddDays(1))
            if (IsWorkingDay(date)) dates.Add(date);

        return dates;
    }

    /// <summary>The dates in a range that do not count, so a request can explain its own total.</summary>
    public static IReadOnlyList<DateOnly> NonWorkingDatesInRange(DateOnly from, DateOnly to)
    {
        var dates = new List<DateOnly>();
        for (var date = from; date <= to; date = date.AddDays(1))
            if (!IsWorkingDay(date)) dates.Add(date);

        return dates;
    }

    private static bool IsWorkingDay(DateOnly date)
        => date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
}
