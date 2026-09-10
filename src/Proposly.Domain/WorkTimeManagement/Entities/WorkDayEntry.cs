using Proposly.Shared.Primitives;

namespace Proposly.Domain.WorkTimeManagement.Entities;

/// <summary>
/// One recorded working day inside a <see cref="Timesheet"/>. Times are entered by the employee;
/// there is no live clock. Worked hours are derived and stored so reports never recompute them.
/// </summary>
public sealed class WorkDayEntry : Entity<Guid>
{
    private WorkDayEntry() { } // For EF Core

    private WorkDayEntry(
        Guid id,
        Guid timesheetId,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        int breakMinutes,
        bool crossesMidnight,
        string? note)
        : base(id)
    {
        TimesheetId = timesheetId;
        Date = date;
        Apply(startTime, endTime, breakMinutes, crossesMidnight, note);
    }

    public static WorkDayEntry Create(
        Guid timesheetId,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        int breakMinutes,
        bool crossesMidnight = false,
        string? note = null)
        => new(Guid.NewGuid(), timesheetId, date, startTime, endTime, breakMinutes, crossesMidnight, note);

    public Guid TimesheetId { get; private set; }
    public DateOnly Date { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public int BreakMinutes { get; private set; }

    /// <summary>
    /// Set when the shift runs past midnight, so an end time at or before the start time is a
    /// night shift rather than a data-entry mistake. The day stays attributed to <see cref="Date"/>.
    /// </summary>
    public bool CrossesMidnight { get; private set; }

    public string? Note { get; private set; }

    /// <summary>Derived on write and stored: the span from start to end, less the break.</summary>
    public decimal WorkedHours { get; private set; }

    public void Update(TimeOnly startTime, TimeOnly endTime, int breakMinutes, bool crossesMidnight, string? note)
        => Apply(startTime, endTime, breakMinutes, crossesMidnight, note);

    private void Apply(TimeOnly startTime, TimeOnly endTime, int breakMinutes, bool crossesMidnight, string? note)
    {
        if (breakMinutes < 0)
            throw new ArgumentException("Break duration cannot be negative.", nameof(breakMinutes));

        if (note?.Length > 500)
            throw new ArgumentException("Note cannot exceed 500 characters.", nameof(note));

        var spanMinutes = CalculateSpanMinutes(startTime, endTime, crossesMidnight);

        if (breakMinutes >= spanMinutes)
            throw new InvalidOperationException(
                "Break duration must be shorter than the time between start and end.");

        StartTime = startTime;
        EndTime = endTime;
        BreakMinutes = breakMinutes;
        CrossesMidnight = crossesMidnight;
        Note = note;
        WorkedHours = Math.Round((decimal)(spanMinutes - breakMinutes) / 60m, 2, MidpointRounding.AwayFromZero);
    }

    private static int CalculateSpanMinutes(TimeOnly startTime, TimeOnly endTime, bool crossesMidnight)
    {
        if (endTime == startTime)
            throw new InvalidOperationException("End time must differ from start time.");

        // TimeOnly subtraction wraps around midnight rather than going negative, so an end at or
        // before the start already yields the correct overnight span. What subtraction cannot tell
        // us is whether that wrap was intended — hence the explicit flag.
        var endsNextDay = endTime < startTime;

        if (endsNextDay && !crossesMidnight)
            throw new InvalidOperationException(
                "End time must be after start time. Mark the day as crossing midnight for a night shift.");

        return (int)(endTime - startTime).TotalMinutes;
    }
}
