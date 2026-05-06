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
        string? description,
        DateOnly date)
        : base(id)
    {
        ProjectId = projectId;
        MemberId = memberId;
        TaskId = taskId;
        HoursWorked = hoursWorked;
        HourlyRateSnapshot = hourlyRateSnapshot;
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
        string? description,
        DateOnly date)
        => new(Guid.NewGuid(), projectId, memberId, taskId, hoursWorked, hourlyRateSnapshot, description, date);

    public void Update(decimal hoursWorked, string? description)
    {
        HoursWorked = hoursWorked;
        Description = description;
    }

    public Guid ProjectId { get; private set; }
    public Guid MemberId { get; private set; }
    public Guid? TaskId { get; private set; }
    public decimal HoursWorked { get; private set; }

    /// <summary>Snapshot of the member's hourly rate at the time of logging.</summary>
    public Money HourlyRateSnapshot { get; private set; } = null!;
    public string? Description { get; private set; }
    public DateOnly Date { get; private set; }
    public DateTime LoggedAt { get; private set; }
}
