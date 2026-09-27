namespace Proposly.Domain.WorkTimeManagement.Enums;

/// <summary>
/// The weekdays an employee normally works, as a set. Stored as a single int column rather than a
/// child table, since it is read on every target-hour calculation.
/// </summary>
[Flags]
public enum WeekDays
{
    None = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 4,
    Thursday = 8,
    Friday = 16,
    Saturday = 32,
    Sunday = 64,

    /// <summary>The common default: Monday to Friday.</summary>
    MondayToFriday = Monday | Tuesday | Wednesday | Thursday | Friday
}

public static class WeekDaysExtensions
{
    public static WeekDays ToWeekDay(this DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => WeekDays.Monday,
        DayOfWeek.Tuesday => WeekDays.Tuesday,
        DayOfWeek.Wednesday => WeekDays.Wednesday,
        DayOfWeek.Thursday => WeekDays.Thursday,
        DayOfWeek.Friday => WeekDays.Friday,
        DayOfWeek.Saturday => WeekDays.Saturday,
        _ => WeekDays.Sunday
    };

    public static bool Includes(this WeekDays pattern, DateOnly date)
        => pattern.HasFlag(date.DayOfWeek.ToWeekDay());

    /// <summary>How many days a week the pattern covers — the divisor for daily hours.</summary>
    public static int Count(this WeekDays pattern)
    {
        var count = 0;
        foreach (WeekDays day in Enum.GetValues<WeekDays>())
        {
            // Skip None and the MondayToFriday composite; only single-day flags count.
            if (day is WeekDays.None or WeekDays.MondayToFriday) continue;
            if (pattern.HasFlag(day)) count++;
        }

        return count;
    }
}
