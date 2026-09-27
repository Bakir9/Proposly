using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Events;

namespace Proposly.Domain.Tests.WorkTimeManagement;

public class TimesheetTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ApproverId = Guid.NewGuid();

    private static Timesheet March() => Timesheet.Create(CompanyId, UserId, 2026, 3);

    private static Timesheet MarchWithOneDay()
    {
        var sheet = March();
        sheet.AddOrUpdateDay(new DateOnly(2026, 3, 2), new TimeOnly(8, 30), new TimeOnly(17, 0), 30);
        return sheet;
    }

    private static Timesheet SubmittedMarch()
    {
        var sheet = MarchWithOneDay();
        sheet.Submit();
        return sheet;
    }

    // --- Creation ---

    [Fact]
    public void A_new_timesheet_starts_as_draft_with_no_days()
    {
        var sheet = March();

        Assert.Equal(TimesheetStatus.Draft, sheet.Status);
        Assert.Empty(sheet.Days);
        Assert.Equal(0m, sheet.TotalWorkedHours);
        Assert.Equal(UserId, sheet.UserId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void An_invalid_month_is_rejected(int month)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Timesheet.Create(CompanyId, UserId, 2026, month));
    }

    // --- Day entries ---

    [Fact]
    public void Recording_a_day_increases_the_monthly_total()
    {
        var sheet = MarchWithOneDay();

        Assert.Single(sheet.Days);
        Assert.Equal(8.0m, sheet.TotalWorkedHours);
    }

    [Fact]
    public void Recording_the_same_date_twice_replaces_rather_than_duplicates()
    {
        var sheet = MarchWithOneDay();

        sheet.AddOrUpdateDay(new DateOnly(2026, 3, 2), new TimeOnly(8, 15), new TimeOnly(16, 45), 45);

        Assert.Single(sheet.Days);
        Assert.Equal(7.75m, sheet.TotalWorkedHours);
    }

    [Fact]
    public void Gaps_are_allowed_and_past_days_can_be_filled_in_retrospectively()
    {
        var sheet = March();

        sheet.AddOrUpdateDay(new DateOnly(2026, 3, 5), new TimeOnly(8, 0), new TimeOnly(16, 0), 30);
        sheet.AddOrUpdateDay(new DateOnly(2026, 3, 2), new TimeOnly(8, 0), new TimeOnly(16, 0), 30);
        sheet.AddOrUpdateDay(new DateOnly(2026, 3, 20), new TimeOnly(8, 0), new TimeOnly(16, 0), 30);

        Assert.Equal(3, sheet.Days.Count);
        Assert.Equal(22.5m, sheet.TotalWorkedHours);
    }

    [Fact]
    public void A_date_outside_the_month_is_rejected()
    {
        var sheet = March();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            sheet.AddOrUpdateDay(new DateOnly(2026, 4, 1), new TimeOnly(8, 0), new TimeOnly(16, 0), 30));

        Assert.Contains("does not fall within 2026-03", ex.Message);
    }

    [Fact]
    public void A_day_far_beyond_every_statutory_limit_is_still_accepted()
    {
        var sheet = March();

        // 15.5 hours with a 15-minute break breaches any plausible daily maximum and break
        // minimum. It MUST still save — breaches are flagged, never blocking (FR-051).
        sheet.AddOrUpdateDay(new DateOnly(2026, 3, 11), new TimeOnly(6, 0), new TimeOnly(21, 30), 15);

        Assert.Single(sheet.Days);
        Assert.Equal(15.25m, sheet.TotalWorkedHours);
    }

    [Fact]
    public void Removing_a_day_reduces_the_total()
    {
        var sheet = MarchWithOneDay();

        sheet.RemoveDay(new DateOnly(2026, 3, 2));

        Assert.Empty(sheet.Days);
        Assert.Equal(0m, sheet.TotalWorkedHours);
    }

    [Fact]
    public void Removing_a_day_that_was_never_recorded_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => March().RemoveDay(new DateOnly(2026, 3, 9)));
    }

    // --- Transitions ---

    [Fact]
    public void Submitting_a_draft_moves_it_to_submitted_and_raises_an_event()
    {
        var sheet = MarchWithOneDay();

        sheet.Submit();

        Assert.Equal(TimesheetStatus.Submitted, sheet.Status);
        Assert.NotNull(sheet.SubmittedAt);
        Assert.Contains(sheet.DomainEvents, e => e is TimesheetSubmittedDomainEvent);
    }

    [Fact]
    public void Submitting_twice_is_rejected()
    {
        var sheet = SubmittedMarch();

        Assert.Throws<InvalidOperationException>(sheet.Submit);
    }

    [Fact]
    public void Approving_a_submitted_timesheet_records_the_approver()
    {
        var sheet = SubmittedMarch();

        sheet.Approve(ApproverId);

        Assert.Equal(TimesheetStatus.Approved, sheet.Status);
        Assert.Equal(ApproverId, sheet.ApprovedById);
        Assert.NotNull(sheet.ApprovedAt);
        Assert.False(sheet.IsSelfApproved);
        Assert.Contains(sheet.DomainEvents, e => e is TimesheetApprovedDomainEvent);
    }

    [Fact]
    public void An_owner_approving_their_own_month_is_flagged_as_self_approved()
    {
        var sheet = SubmittedMarch();

        sheet.Approve(UserId);

        Assert.True(sheet.IsSelfApproved);
    }

    [Fact]
    public void Approving_a_draft_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => MarchWithOneDay().Approve(ApproverId));
    }

    [Fact]
    public void Returning_a_submitted_timesheet_sends_it_back_to_draft()
    {
        var sheet = SubmittedMarch();

        sheet.ReturnForCorrection(ApproverId, "Thursday looks wrong");

        Assert.Equal(TimesheetStatus.Draft, sheet.Status);
        Assert.Null(sheet.SubmittedAt);
        Assert.Contains(sheet.DomainEvents, e => e is TimesheetReturnedForCorrectionDomainEvent);
    }

    [Fact]
    public void Returning_a_draft_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => MarchWithOneDay().ReturnForCorrection(ApproverId));
    }

    [Fact]
    public void Locking_an_approved_timesheet_closes_the_period()
    {
        var sheet = SubmittedMarch();
        sheet.Approve(ApproverId);

        sheet.Lock();

        Assert.Equal(TimesheetStatus.Locked, sheet.Status);
        Assert.NotNull(sheet.LockedAt);
    }

    [Fact]
    public void Locking_a_submitted_timesheet_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(SubmittedMarch().Lock);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Reopening_an_approved_or_locked_timesheet_returns_it_to_draft(bool lockFirst)
    {
        var sheet = SubmittedMarch();
        sheet.Approve(ApproverId);
        if (lockFirst) sheet.Lock();

        sheet.Reopen(ApproverId);

        Assert.Equal(TimesheetStatus.Draft, sheet.Status);
        Assert.Equal(ApproverId, sheet.ReopenedById);
        Assert.NotNull(sheet.ReopenedAt);
        Assert.Null(sheet.ApprovedAt);
        Assert.Null(sheet.LockedAt);
        Assert.False(sheet.IsSelfApproved);
    }

    [Fact]
    public void Reopening_a_draft_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => MarchWithOneDay().Reopen(ApproverId));
    }

    // --- Edits are refused outside Draft ---

    [Theory]
    [InlineData(TimesheetStatus.Submitted)]
    [InlineData(TimesheetStatus.Approved)]
    [InlineData(TimesheetStatus.Locked)]
    public void Day_entries_cannot_be_changed_outside_draft(TimesheetStatus status)
    {
        var sheet = SubmittedMarch();
        if (status is TimesheetStatus.Approved or TimesheetStatus.Locked) sheet.Approve(ApproverId);
        if (status is TimesheetStatus.Locked) sheet.Lock();

        var addEx = Assert.Throws<InvalidOperationException>(() =>
            sheet.AddOrUpdateDay(new DateOnly(2026, 3, 3), new TimeOnly(8, 0), new TimeOnly(16, 0), 30));
        Assert.Contains("only be changed while the timesheet is Draft", addEx.Message);

        Assert.Throws<InvalidOperationException>(() => sheet.RemoveDay(new DateOnly(2026, 3, 2)));
    }

    [Fact]
    public void A_reopened_timesheet_accepts_edits_again()
    {
        var sheet = SubmittedMarch();
        sheet.Approve(ApproverId);
        sheet.Lock();
        sheet.Reopen(ApproverId);

        sheet.AddOrUpdateDay(new DateOnly(2026, 3, 3), new TimeOnly(8, 0), new TimeOnly(16, 0), 30);

        Assert.Equal(2, sheet.Days.Count);
    }
}
