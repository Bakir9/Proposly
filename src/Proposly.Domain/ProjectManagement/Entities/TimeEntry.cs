using Proposly.Shared.Primitives;
using Proposly.Shared.ValueObjects;

namespace Proposly.Domain.ProjectManagement.Entities;

public sealed class TimeEntry : Entity<Guid>
{
    private TimeEntry() { } // For EF Core

    private TimeEntry(
        Guid id,
        Guid projectId,
        Guid memberId,
        Guid? taskId,
        decimal hoursWorked,
        Money hourlyRateSnapshot,
        Money costRateSnapshot,
        Money billRateSnapshot,
        string? description,
        DateOnly date)
        : base(id)
    {
        ProjectId = projectId;
        MemberId = memberId;
        TaskId = taskId;
        HoursWorked = hoursWorked;
        HourlyRateSnapshot = hourlyRateSnapshot;
        CostRateSnapshot = costRateSnapshot;
        BillRateSnapshot = billRateSnapshot;
        Description = description;
        Date = date;
        LoggedAt = DateTime.UtcNow;
    }

    public static TimeEntry Create(
        Guid projectId,
        Guid memberId,
        Guid? taskId,
        decimal hoursWorked,
        Money hourlyRateSnapshot,
        Money costRateSnapshot,
        Money billRateSnapshot,
        string? description,
        DateOnly date)
        => new(Guid.NewGuid(), projectId, memberId, taskId, hoursWorked, hourlyRateSnapshot, costRateSnapshot, billRateSnapshot, description, date);

    public void Update(decimal hoursWorked, string? description)
    {
        HoursWorked = hoursWorked;
        Description = description;
    }

    public Guid ProjectId { get; private set; }
    public Guid MemberId { get; private set; }
    public Guid? TaskId { get; private set; }
    public decimal HoursWorked { get; private set; }

    /// <summary>Legacy snapshot (equals the cost rate for new entries) — kept for old reads.</summary>
    public Money HourlyRateSnapshot { get; private set; } = null!;

    /// <summary>What this hour cost the company — snapshotted so rate changes never rewrite history.</summary>
    public Money CostRateSnapshot { get; private set; } = null!;

    /// <summary>What the client pays for this hour on T&amp;M — snapshotted at logging time.</summary>
    public Money BillRateSnapshot { get; private set; } = null!;
    public string? Description { get; private set; }
    public DateOnly Date { get; private set; }
    public DateTime LoggedAt { get; private set; }
}
