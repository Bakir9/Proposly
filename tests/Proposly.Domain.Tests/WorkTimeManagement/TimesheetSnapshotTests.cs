using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;

namespace Proposly.Domain.Tests.WorkTimeManagement;

/// <summary>
/// The month-end figures are frozen at approval. Nothing that happens afterwards — a changed
/// contract, a corrected holiday, a new policy — may move a number that has been reported.
/// </summary>
public class TimesheetSnapshotTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ApproverId = Guid.NewGuid();

    private static Timesheet ApprovedMarch()
    {
        var sheet = Timesheet.Create(CompanyId, UserId, 2026, 3);
        sheet.AddOrUpdateDay(new DateOnly(2026, 3, 2), new TimeOnly(8, 0), new TimeOnly(16, 30), 30);
        sheet.Submit();
        sheet.Approve(ApproverId);
        return sheet;
    }

    [Fact]
    public void An_open_month_has_no_frozen_figures()
    {
        var sheet = Timesheet.Create(CompanyId, UserId, 2026, 3);

        Assert.Null(sheet.TargetHoursSnapshot);
        Assert.Null(sheet.ClosingBalanceHours);
        Assert.Null(sheet.ForfeitedHours);
        Assert.False(sheet.IsRevised);
    }

    [Fact]
    public void Approval_freezes_the_figures()
    {
        var sheet = ApprovedMarch();

        sheet.ApplySnapshot(
            targetHours: 161.7m, actualHours: 168m,
            openingBalance: 74m, closingBalance: 80m, forfeitedHours: 0.3m);

        Assert.Equal(161.7m, sheet.TargetHoursSnapshot);
        Assert.Equal(168m, sheet.ActualHoursSnapshot);
        Assert.Equal(74m, sheet.OpeningBalanceHours);
        Assert.Equal(80m, sheet.ClosingBalanceHours);
        Assert.Equal(0.3m, sheet.ForfeitedHours);
    }

    [Fact]
    public void Figures_cannot_be_frozen_before_approval()
    {
        var sheet = Timesheet.Create(CompanyId, UserId, 2026, 3);
        sheet.AddOrUpdateDay(new DateOnly(2026, 3, 2), new TimeOnly(8, 0), new TimeOnly(16, 30), 30);

        Assert.Throws<InvalidOperationException>(
            () => sheet.ApplySnapshot(161.7m, 8m, 0m, 0m, 0m));

        sheet.Submit();

        Assert.Throws<InvalidOperationException>(
            () => sheet.ApplySnapshot(161.7m, 8m, 0m, 0m, 0m));
    }

    [Fact]
    public void Figures_cannot_be_frozen_again_after_the_month_is_locked()
    {
        var sheet = ApprovedMarch();
        sheet.ApplySnapshot(161.7m, 168m, 74m, 80m, 0.3m);
        sheet.Lock();

        Assert.Throws<InvalidOperationException>(
            () => sheet.ApplySnapshot(100m, 100m, 0m, 0m, 0m));

        // The reported figures stand.
        Assert.Equal(161.7m, sheet.TargetHoursSnapshot);
        Assert.Equal(80m, sheet.ClosingBalanceHours);
    }

    // --- Revision ---

    [Fact]
    public void A_reported_month_can_be_marked_revised_without_its_figures_moving()
    {
        var sheet = ApprovedMarch();
        sheet.ApplySnapshot(161.7m, 168m, 74m, 80m, 0.3m);

        sheet.MarkRevised();

        Assert.True(sheet.IsRevised);
        Assert.Equal(161.7m, sheet.TargetHoursSnapshot);
        Assert.Equal(80m, sheet.ClosingBalanceHours);
        Assert.Equal(TimesheetStatus.Approved, sheet.Status);
    }

    [Fact]
    public void A_locked_month_can_also_be_marked_revised()
    {
        var sheet = ApprovedMarch();
        sheet.ApplySnapshot(161.7m, 168m, 74m, 80m, 0m);
        sheet.Lock();

        sheet.MarkRevised();

        Assert.True(sheet.IsRevised);
        Assert.Equal(TimesheetStatus.Locked, sheet.Status);
    }

    [Fact]
    public void Marking_an_open_month_revised_does_nothing()
    {
        // Nothing has been reported yet, so there is nothing to flag.
        var sheet = Timesheet.Create(CompanyId, UserId, 2026, 3);

        sheet.MarkRevised();

        Assert.False(sheet.IsRevised);
    }

    // --- Reopening ---

    [Fact]
    public void Reopening_clears_the_frozen_figures_and_the_revised_flag()
    {
        var sheet = ApprovedMarch();
        sheet.ApplySnapshot(161.7m, 168m, 74m, 80m, 0.3m);
        sheet.MarkRevised();

        sheet.Reopen(ApproverId);

        // The month is open again, so everything is recomputed on read and frozen afresh on the
        // next approval. Stale figures would be worse than none.
        Assert.Null(sheet.TargetHoursSnapshot);
        Assert.Null(sheet.ActualHoursSnapshot);
        Assert.Null(sheet.OpeningBalanceHours);
        Assert.Null(sheet.ClosingBalanceHours);
        Assert.Null(sheet.ForfeitedHours);
        Assert.False(sheet.IsRevised);
    }

    [Fact]
    public void A_reopened_month_can_be_approved_and_refrozen()
    {
        var sheet = ApprovedMarch();
        sheet.ApplySnapshot(161.7m, 168m, 74m, 80m, 0.3m);
        sheet.Reopen(ApproverId);

        sheet.AddOrUpdateDay(new DateOnly(2026, 3, 3), new TimeOnly(8, 0), new TimeOnly(16, 30), 30);
        sheet.Submit();
        sheet.Approve(ApproverId);
        sheet.ApplySnapshot(161.7m, 176m, 74m, 80m, 8.3m);

        Assert.Equal(176m, sheet.ActualHoursSnapshot);
        Assert.Equal(8.3m, sheet.ForfeitedHours);
    }
}
