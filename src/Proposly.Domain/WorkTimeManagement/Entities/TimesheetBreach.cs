using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Shared.Primitives;

namespace Proposly.Domain.WorkTimeManagement.Entities;

/// <summary>
/// One detected limit breach, snapshotted onto a timesheet at approval.
/// <para>
/// While a month is open, breaches are computed on read and never stored, so a corrected day
/// clears its flag immediately. At approval the evaluated set is frozen here with the limit that
/// applied, so a later policy change cannot rewrite what an approver signed off on.
/// </para>
/// </summary>
public sealed class TimesheetBreach : Entity<Guid>
{
    private TimesheetBreach() { } // For EF Core

    private TimesheetBreach(
        Guid id,
        Guid timesheetId,
        BreachKind kind,
        DateOnly? date,
        DateOnly? weekStartDate,
        decimal limitValue,
        decimal actualValue)
        : base(id)
    {
        TimesheetId = timesheetId;
        Kind = kind;
        Date = date;
        WeekStartDate = weekStartDate;
        LimitValue = limitValue;
        ActualValue = actualValue;
    }

    /// <summary>A breach scoped to one day — daily maximum, break, or rest.</summary>
    public static TimesheetBreach ForDay(
        Guid timesheetId, BreachKind kind, DateOnly date, decimal limitValue, decimal actualValue)
        => new(Guid.NewGuid(), timesheetId, kind, date, null, limitValue, actualValue);

    /// <summary>A breach scoped to a week — weekly maximum or the averaging window.</summary>
    public static TimesheetBreach ForWeek(
        Guid timesheetId, BreachKind kind, DateOnly weekStartDate, decimal limitValue, decimal actualValue)
        => new(Guid.NewGuid(), timesheetId, kind, null, weekStartDate, limitValue, actualValue);

    public Guid TimesheetId { get; private set; }
    public BreachKind Kind { get; private set; }
    public DateOnly? Date { get; private set; }
    public DateOnly? WeekStartDate { get; private set; }

    /// <summary>The limit that applied when the breach was recorded.</summary>
    public decimal LimitValue { get; private set; }

    public decimal ActualValue { get; private set; }

    public Guid? AcknowledgedById { get; private set; }
    public DateTime? AcknowledgedAt { get; private set; }

    public void Acknowledge(Guid approverId)
    {
        AcknowledgedById = approverId;
        AcknowledgedAt = DateTime.UtcNow;
    }
}
