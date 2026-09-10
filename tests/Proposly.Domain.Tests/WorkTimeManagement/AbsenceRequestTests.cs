using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Events;
using Proposly.Domain.WorkTimeManagement.Services;

namespace Proposly.Domain.Tests.WorkTimeManagement;

public class AbsenceRequestTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ApproverId = Guid.NewGuid();

    // Mon 6 July 2026 to Fri 17 July 2026 — two full working weeks.
    private static readonly DateOnly Start = new(2026, 7, 6);
    private static readonly DateOnly End = new(2026, 7, 17);
    private static readonly DateOnly BeforeStart = new(2026, 6, 1);
    private static readonly DateOnly AfterStart = new(2026, 7, 8);

    private static AbsenceRequest Vacation(
        DateOnly? start = null, DateOnly? end = null,
        bool firstHalf = false, bool lastHalf = false)
    {
        var from = start ?? Start;
        var to = end ?? End;

        return AbsenceRequest.Create(
            CompanyId, UserId, AbsenceType.Vacation, from, to, firstHalf, lastHalf,
            WorkingDayCalculator.ConsumedDays(from, to, firstHalf, lastHalf));
    }

    private static AbsenceRequest Of(AbsenceType type)
        => AbsenceRequest.Create(
            CompanyId, UserId, type, Start, End, false, false,
            WorkingDayCalculator.ConsumedDays(Start, End, false, false));

    // --- Creation ---

    [Fact]
    public void A_new_request_is_pending_and_raises_an_event()
    {
        var request = Vacation();

        Assert.Equal(AbsenceStatus.Pending, request.Status);
        Assert.Equal(10m, request.ConsumedDays); // weekends excluded
        Assert.Contains(request.DomainEvents, e => e is AbsenceRequestedDomainEvent);
    }

    [Fact]
    public void An_end_date_before_the_start_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => AbsenceRequest.Create(
            CompanyId, UserId, AbsenceType.Vacation, End, Start, false, false, 1m));
    }

    [Fact]
    public void A_reason_longer_than_500_characters_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => AbsenceRequest.Create(
            CompanyId, UserId, AbsenceType.Vacation, Start, End, false, false, 10m,
            new string('x', 501)));
    }

    [Fact]
    public void Only_vacation_consumes_entitlement()
    {
        Assert.True(Of(AbsenceType.Vacation).ConsumesEntitlement);

        Assert.False(Of(AbsenceType.SickLeave).ConsumesEntitlement);
        Assert.False(Of(AbsenceType.UnpaidLeave).ConsumesEntitlement);
        Assert.False(Of(AbsenceType.ParentalLeave).ConsumesEntitlement);
        Assert.False(Of(AbsenceType.SpecialLeave).ConsumesEntitlement);
    }

    [Fact]
    public void Sick_leave_stores_only_its_type_and_dates()
    {
        var request = Of(AbsenceType.SickLeave);

        // No property on the entity can hold health information beyond the type itself.
        var propertyNames = typeof(AbsenceRequest).GetProperties().Select(p => p.Name).ToList();

        Assert.DoesNotContain("Diagnosis", propertyNames);
        Assert.DoesNotContain("MedicalNote", propertyNames);
        Assert.DoesNotContain("Certificate", propertyNames);
        Assert.Equal(AbsenceType.SickLeave, request.Type);
        Assert.Null(request.Reason);
    }

    // --- Approval ---

    [Fact]
    public void Approving_a_pending_request_records_the_approver()
    {
        var request = Vacation();

        request.Approve(ApproverId);

        Assert.Equal(AbsenceStatus.Approved, request.Status);
        Assert.Equal(ApproverId, request.ApproverId);
        Assert.NotNull(request.DecidedAt);
        Assert.Contains(request.DomainEvents, e => e is AbsenceApprovedDomainEvent);
    }

    [Fact]
    public void Rejecting_a_pending_request_records_the_reason()
    {
        var request = Vacation();

        request.Reject(ApproverId, "Two people already away that week");

        Assert.Equal(AbsenceStatus.Rejected, request.Status);
        Assert.Equal("Two people already away that week", request.DecisionReason);
        Assert.Contains(request.DomainEvents, e => e is AbsenceRejectedDomainEvent);
    }

    [Fact]
    public void A_rejection_needs_a_reason()
    {
        var request = Vacation();

        Assert.Throws<ArgumentException>(() => request.Reject(ApproverId, "  "));
        Assert.Equal(AbsenceStatus.Pending, request.Status);
    }

    [Fact]
    public void A_second_decision_on_a_decided_request_is_refused()
    {
        // The first approver wins; the second is told it has already been decided.
        var approved = Vacation();
        approved.Approve(ApproverId);

        var ex = Assert.Throws<InvalidOperationException>(() => approved.Approve(Guid.NewGuid()));
        Assert.Contains("already been decided", ex.Message);

        Assert.Throws<InvalidOperationException>(() => approved.Reject(ApproverId, "too late"));
    }

    [Fact]
    public void A_rejected_request_cannot_later_be_approved()
    {
        var request = Vacation();
        request.Reject(ApproverId, "no");

        Assert.Throws<InvalidOperationException>(() => request.Approve(ApproverId));
    }

    // --- Cancellation ---

    [Fact]
    public void A_pending_request_can_be_cancelled()
    {
        var request = Vacation();

        request.Cancel(BeforeStart);

        Assert.Equal(AbsenceStatus.Cancelled, request.Status);
    }

    [Fact]
    public void An_approved_future_absence_can_be_cancelled()
    {
        var request = Vacation();
        request.Approve(ApproverId);

        request.Cancel(BeforeStart);

        Assert.Equal(AbsenceStatus.Cancelled, request.Status);
    }

    [Fact]
    public void An_approved_absence_that_has_started_cannot_be_cancelled()
    {
        var request = Vacation();
        request.Approve(ApproverId);

        var ex = Assert.Throws<InvalidOperationException>(() => request.Cancel(AfterStart));

        Assert.Contains("already started", ex.Message);
        Assert.Equal(AbsenceStatus.Approved, request.Status);
    }

    [Fact]
    public void A_rejected_or_cancelled_request_cannot_be_cancelled_again()
    {
        var rejected = Vacation();
        rejected.Reject(ApproverId, "no");
        Assert.Throws<InvalidOperationException>(() => rejected.Cancel(BeforeStart));

        var cancelled = Vacation();
        cancelled.Cancel(BeforeStart);
        Assert.Throws<InvalidOperationException>(() => cancelled.Cancel(BeforeStart));
    }

    // --- Coverage ---

    [Fact]
    public void A_standing_request_covers_the_dates_in_its_range()
    {
        var request = Vacation();

        Assert.True(request.CoversDate(Start));
        Assert.True(request.CoversDate(new DateOnly(2026, 7, 10)));
        Assert.True(request.CoversDate(End));
        Assert.False(request.CoversDate(new DateOnly(2026, 7, 20)));
    }

    [Fact]
    public void A_cancelled_request_covers_nothing()
    {
        var request = Vacation();
        request.Cancel(BeforeStart);

        Assert.False(request.CoversDate(Start));
    }
}
