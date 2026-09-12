using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Events;
using Proposly.Shared.Interfaces;
using Proposly.Shared.Primitives;

namespace Proposly.Domain.WorkTimeManagement.Entities;

/// <summary>
/// One employee's request to be away.
/// <para>
/// <b>Privacy:</b> there is deliberately no field for a diagnosis, medical note, or any other
/// health detail. Sick leave is identifiable by <see cref="Type"/> and its dates alone, which is
/// what keeps this out of GDPR Article 9 territory. <see cref="Reason"/> is optional free text the
/// employee supplies for their own purposes and is never required for sick leave — do not add a
/// medical field here.
/// </para>
/// </summary>
public sealed class AbsenceRequest : AggregateRoot<Guid>, ITenantEntity, IAuditableEntity, IUserOwnedEntity
{
    private AbsenceRequest() { } // For EF Core

    private AbsenceRequest(
        Guid id,
        Guid companyId,
        Guid userId,
        AbsenceType type,
        DateOnly startDate,
        DateOnly endDate,
        bool firstDayIsHalf,
        bool lastDayIsHalf,
        decimal consumedDays,
        string? reason)
        : base(id)
    {
        CompanyId = companyId;
        UserId = userId;
        Type = type;
        StartDate = startDate;
        EndDate = endDate;
        FirstDayIsHalf = firstDayIsHalf;
        LastDayIsHalf = lastDayIsHalf;
        ConsumedDays = consumedDays;
        Reason = reason;
        Status = AbsenceStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static AbsenceRequest Create(
        Guid companyId,
        Guid userId,
        AbsenceType type,
        DateOnly startDate,
        DateOnly endDate,
        bool firstDayIsHalf,
        bool lastDayIsHalf,
        decimal consumedDays,
        string? reason = null)
    {
        if (endDate < startDate)
            throw new ArgumentException("End date cannot precede start date.", nameof(endDate));

        if (consumedDays < 0)
            throw new ArgumentException("Consumed days cannot be negative.", nameof(consumedDays));

        if (reason?.Length > 500)
            throw new ArgumentException("Reason cannot exceed 500 characters.", nameof(reason));

        var request = new AbsenceRequest(
            Guid.NewGuid(), companyId, userId, type, startDate, endDate,
            firstDayIsHalf, lastDayIsHalf, consumedDays, reason);

        request.RaiseDomainEvent(new AbsenceRequestedDomainEvent(
            request.Id, companyId, userId, type, startDate, endDate, consumedDays));

        return request;
    }

    public Guid CompanyId { get; private set; }
    public Guid UserId { get; private set; }
    public AbsenceType Type { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public bool FirstDayIsHalf { get; private set; }
    public bool LastDayIsHalf { get; private set; }

    /// <summary>
    /// Working days this absence costs, frozen when the request was made. Snapshotted rather than
    /// recomputed so a later calendar or contract change cannot silently alter a decided request.
    /// </summary>
    public decimal ConsumedDays { get; private set; }

    public AbsenceStatus Status { get; private set; }

    /// <summary>Optional, non-medical, employee-supplied note.</summary>
    public string? Reason { get; private set; }

    public Guid? ApproverId { get; private set; }
    public DateTime? DecidedAt { get; private set; }
    public string? DecisionReason { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>Only vacation draws down annual entitlement.</summary>
    public bool ConsumesEntitlement => Type == AbsenceType.Vacation;

    public void Approve(Guid approverId)
    {
        EnsurePending("approved");

        Status = AbsenceStatus.Approved;
        ApproverId = approverId;
        DecidedAt = DateTime.UtcNow;
        Touch();
        RaiseDomainEvent(new AbsenceApprovedDomainEvent(
            Id, CompanyId, UserId, Type, StartDate, EndDate, approverId));
    }

    public void Reject(Guid approverId, string reason)
    {
        EnsurePending("rejected");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A rejection needs a reason.", nameof(reason));

        Status = AbsenceStatus.Rejected;
        ApproverId = approverId;
        DecidedAt = DateTime.UtcNow;
        DecisionReason = reason;
        Touch();
        RaiseDomainEvent(new AbsenceRejectedDomainEvent(
            Id, CompanyId, UserId, Type, StartDate, EndDate, approverId, reason));
    }

    /// <summary>
    /// Withdraws a request. Pending requests can always be cancelled; an approved one only while
    /// it has not started, since an absence already under way is a fact rather than a plan.
    /// </summary>
    public void Cancel(DateOnly today)
    {
        switch (Status)
        {
            case AbsenceStatus.Pending:
                break;

            case AbsenceStatus.Approved when StartDate > today:
                break;

            case AbsenceStatus.Approved:
                throw new InvalidOperationException(
                    "An absence that has already started cannot be cancelled.");

            default:
                throw new InvalidOperationException(
                    $"An absence in '{Status}' status cannot be cancelled.");
        }

        Status = AbsenceStatus.Cancelled;
        Touch();
    }

    /// <summary>True when the range covers <paramref name="date"/> and the absence still stands.</summary>
    public bool CoversDate(DateOnly date)
        => Status is AbsenceStatus.Pending or AbsenceStatus.Approved
           && date >= StartDate && date <= EndDate;

    /// <summary>
    /// How much of a day this absence takes: 1, 0.5 for a half-day boundary, or 0 when the date
    /// is not covered. Used wherever absence has to net off against a day's expected hours.
    /// </summary>
    public decimal DayFraction(DateOnly date)
    {
        if (!CoversDate(date)) return 0m;

        // A single-day absence marked half either way is half a day, not none.
        if (StartDate == EndDate)
            return FirstDayIsHalf || LastDayIsHalf ? 0.5m : 1m;

        if (date == StartDate && FirstDayIsHalf) return 0.5m;
        if (date == EndDate && LastDayIsHalf) return 0.5m;

        return 1m;
    }

    private void EnsurePending(string action)
    {
        if (Status != AbsenceStatus.Pending)
            throw new InvalidOperationException(
                $"An absence in '{Status}' status cannot be {action}. It has already been decided.");
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
