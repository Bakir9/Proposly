namespace Proposly.Domain.WorkTimeManagement.Enums;

/// <summary>
/// The statutory limits checked against recorded working time. A breach is always recorded and
/// surfaced — never a reason to refuse the entry.
/// </summary>
public enum BreachKind
{
    /// <summary>Worked hours on one day exceeded the daily maximum.</summary>
    DailyMaximum,

    /// <summary>Worked hours in one calendar week exceeded the weekly maximum.</summary>
    WeeklyMaximum,

    /// <summary>Average weekly hours over the rolling averaging window exceeded its cap.</summary>
    AveragingWindow,

    /// <summary>The recorded break was shorter than the minimum for a day of that length.</summary>
    InsufficientBreak,

    /// <summary>Too little rest between the end of one working day and the start of the next.</summary>
    InsufficientDailyRest
}
