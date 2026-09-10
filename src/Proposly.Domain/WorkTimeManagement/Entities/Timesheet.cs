using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Events;
using Proposly.Shared.Interfaces;
using Proposly.Shared.Primitives;

namespace Proposly.Domain.WorkTimeManagement.Entities;

/// <summary>
/// One employee's working time for one calendar month — the statutory record.
/// <para>
/// Implements <see cref="IUserOwnedEntity"/>, so AppDbContext scopes rows to the owning employee
/// automatically; only Owners and Admins see other people's timesheets.
/// </para>
/// <para>
/// Bounded to at most 31 day entries, so the whole aggregate loads safely — unlike Project, this
/// needs no read/write split.
/// </para>
/// </summary>
public sealed class Timesheet : AggregateRoot<Guid>, ITenantEntity, IAuditableEntity, IUserOwnedEntity
{
    private readonly List<WorkDayEntry> _days = [];
    private readonly List<TimesheetBreach> _breaches = [];

    private Timesheet() { } // For EF Core

    private Timesheet(Guid id, Guid companyId, Guid userId, int year, int month)
        : base(id)
    {
        CompanyId = companyId;
        UserId = userId;
        Year = year;
        Month = month;
        Status = TimesheetStatus.Draft;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Timesheet Create(Guid companyId, Guid userId, int year, int month)
    {
        if (month is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");

        if (year is < 2000 or > 2100)
            throw new ArgumentOutOfRangeException(nameof(year), "Year is outside the supported range.");

        return new Timesheet(Guid.NewGuid(), companyId, userId, year, month);
    }

    public Guid CompanyId { get; private set; }
    public Guid UserId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public TimesheetStatus Status { get; private set; }

    public DateTime? SubmittedAt { get; private set; }
    public Guid? ApprovedById { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? LockedAt { get; private set; }
    public Guid? ReopenedById { get; private set; }
    public DateTime? ReopenedAt { get; private set; }

    /// <summary>True when the approver was the employee themselves — surfaced on the report.</summary>
    public bool IsSelfApproved { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<WorkDayEntry> Days => _days.AsReadOnly();

    /// <summary>
    /// Breaches acknowledged at approval. Empty while the month is open, where breaches are
    /// computed on read instead so a corrected day clears its flag immediately.
    /// </summary>
    public IReadOnlyCollection<TimesheetBreach> Breaches => _breaches.AsReadOnly();

    public decimal TotalWorkedHours => _days.Sum(d => d.WorkedHours);

    // --- Day entries (only while Draft) ---

    /// <summary>
    /// Records or replaces one day. Gaps are permitted and past dates within the month are
    /// accepted, so a week or a whole month can be filled in retrospectively.
    /// <para>
    /// Deliberately does not consult the working time policy: a day that breaches a statutory
    /// limit must still be saved, and is flagged elsewhere. Refusing it would make the statutory
    /// record false and push employees toward under-reporting.
    /// </para>
    /// </summary>
    public WorkDayEntry AddOrUpdateDay(
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        int breakMinutes,
        bool crossesMidnight = false,
        string? note = null)
    {
        EnsureDraft();
        EnsureDateInPeriod(date);

        var existing = _days.FirstOrDefault(d => d.Date == date);
        if (existing is not null)
        {
            existing.Update(startTime, endTime, breakMinutes, crossesMidnight, note);
            Touch();
            return existing;
        }

        var entry = WorkDayEntry.Create(Id, date, startTime, endTime, breakMinutes, crossesMidnight, note);
        _days.Add(entry);
        Touch();
        return entry;
    }

    public void RemoveDay(DateOnly date)
    {
        EnsureDraft();

        var entry = _days.FirstOrDefault(d => d.Date == date)
            ?? throw new InvalidOperationException($"No entry recorded for {date:yyyy-MM-dd}.");

        _days.Remove(entry);
        Touch();
    }

    // --- Status transitions ---

    public void Submit()
    {
        if (Status != TimesheetStatus.Draft)
            throw new InvalidOperationException($"Cannot submit a timesheet in '{Status}' status.");

        Status = TimesheetStatus.Submitted;
        SubmittedAt = DateTime.UtcNow;
        Touch();
        RaiseDomainEvent(new TimesheetSubmittedDomainEvent(Id, CompanyId, UserId, Year, Month));
    }

    /// <summary>
    /// Approver sign-off. Any breaches evaluated for the month are snapshotted here, so what the
    /// approver accepted is preserved even if the policy later changes.
    /// </summary>
    /// <param name="breaches">
    /// Breaches evaluated for this month, from <c>ComplianceEvaluator</c>. When any are present the
    /// approver must acknowledge them explicitly.
    /// </param>
    public void Approve(
        Guid approverId,
        IReadOnlyCollection<TimesheetBreach>? breaches = null,
        bool breachesAcknowledged = false)
    {
        if (Status != TimesheetStatus.Submitted)
            throw new InvalidOperationException($"Cannot approve a timesheet in '{Status}' status.");

        var outstanding = breaches ?? [];

        if (outstanding.Count > 0 && !breachesAcknowledged)
            throw new InvalidOperationException(
                $"This month has {outstanding.Count} working time breach(es). " +
                "They must be acknowledged before it can be approved.");

        _breaches.Clear();
        foreach (var breach in outstanding)
        {
            breach.Acknowledge(approverId);
            _breaches.Add(breach);
        }

        Status = TimesheetStatus.Approved;
        ApprovedById = approverId;
        ApprovedAt = DateTime.UtcNow;
        IsSelfApproved = approverId == UserId;
        Touch();
        RaiseDomainEvent(new TimesheetApprovedDomainEvent(Id, CompanyId, UserId, Year, Month, approverId));
    }

    public void ReturnForCorrection(Guid approverId, string? reason = null)
    {
        if (Status != TimesheetStatus.Submitted)
            throw new InvalidOperationException($"Only a submitted timesheet can be returned for correction.");

        Status = TimesheetStatus.Draft;
        SubmittedAt = null;
        Touch();
        RaiseDomainEvent(new TimesheetReturnedForCorrectionDomainEvent(
            Id, CompanyId, UserId, Year, Month, approverId, reason));
    }

    /// <summary>Payroll close. Distinct from approval so a closed period can be told apart.</summary>
    public void Lock()
    {
        if (Status != TimesheetStatus.Approved)
            throw new InvalidOperationException($"Cannot lock a timesheet in '{Status}' status.");

        Status = TimesheetStatus.Locked;
        LockedAt = DateTime.UtcNow;
        Touch();
    }

    public void Reopen(Guid approverId)
    {
        if (Status is not (TimesheetStatus.Approved or TimesheetStatus.Locked))
            throw new InvalidOperationException($"Cannot reopen a timesheet in '{Status}' status.");

        Status = TimesheetStatus.Draft;
        SubmittedAt = null;
        ApprovedById = null;
        ApprovedAt = null;
        LockedAt = null;
        IsSelfApproved = false;
        ReopenedById = approverId;
        ReopenedAt = DateTime.UtcNow;

        // The snapshotted breaches belonged to the withdrawn approval. While the month is open
        // again they are recomputed on read, and re-acknowledged on the next approval.
        _breaches.Clear();

        Touch();
    }

    // --- Guards ---

    private void EnsureDraft()
    {
        if (Status != TimesheetStatus.Draft)
            throw new InvalidOperationException(
                $"Day entries can only be changed while the timesheet is Draft (currently '{Status}').");
    }

    private void EnsureDateInPeriod(DateOnly date)
    {
        if (date.Year != Year || date.Month != Month)
            throw new InvalidOperationException(
                $"{date:yyyy-MM-dd} does not fall within {Year}-{Month:00}.");
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
