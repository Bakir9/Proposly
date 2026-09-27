using NSubstitute;
using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Commands.ApproveTimesheet;
using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Services;

namespace Proposly.Application.Tests.WorkTimeManagement;

/// <summary>
/// An approver must see and accept working time breaches explicitly. The breach set is evaluated
/// server-side, so the browser cannot shrink it by sending back a shorter list.
/// </summary>
public class ApproveWithBreachesTests
{
    private readonly ITimesheetRepository _timesheets = Substitute.For<ITimesheetRepository>();
    private readonly IWorkTimePolicyRepository _policies = Substitute.For<IWorkTimePolicyRepository>();
    private readonly IAbsenceRepository _absences = Substitute.For<IAbsenceRepository>();
    private readonly IEmploymentTermsRepository _terms = Substitute.For<IEmploymentTermsRepository>();
    private readonly INonWorkingDayRepository _calendar = Substitute.For<INonWorkingDayRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ApproverId = Guid.NewGuid();

    public ApproveWithBreachesTests()
    {
        _currentUser.CompanyId.Returns(CompanyId);
        _currentUser.UserId.Returns(ApproverId);
        _currentUser.CanViewAllEmployees.Returns(true);

        _timesheets.GetDaysInRangeAsync(
            Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);

        _absences.GetApprovedInRangeAsync(
            Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);

        // No terms and an empty calendar: the approval snapshot computes a null target, which is
        // fine here — these tests are about the breach acknowledgement, not the figures.
        _terms.GetForRangeAsync(
            Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);

        _calendar.GetForRangeAsync(
            Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);
    }

    private void WithAustrianPolicy()
        => _policies.GetEffectiveAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(WorkTimePolicyDefaults.For("AT", CompanyId, new DateOnly(2026, 1, 1)));

    private void WithNoPolicy()
        => _policies.GetEffectiveAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((WorkTimePolicy?)null);

    /// <summary>A submitted month with one 13-hour day — over the Austrian 12h daily maximum.</summary>
    private Timesheet SubmittedMonthWithABreach()
    {
        var sheet = Timesheet.Create(CompanyId, UserId, 2026, 3);
        sheet.AddOrUpdateDay(new DateOnly(2026, 3, 11), new TimeOnly(6, 0), new TimeOnly(19, 30), 30);
        sheet.Submit();
        _timesheets.GetByIdAsync(sheet.Id).Returns(sheet);
        return sheet;
    }

    private Timesheet SubmittedCompliantMonth()
    {
        var sheet = Timesheet.Create(CompanyId, UserId, 2026, 3);
        sheet.AddOrUpdateDay(new DateOnly(2026, 3, 2), new TimeOnly(8, 0), new TimeOnly(16, 30), 30);
        sheet.Submit();
        _timesheets.GetByIdAsync(sheet.Id).Returns(sheet);
        return sheet;
    }

    private ApproveTimesheetCommandHandler Handler()
        => new(_timesheets, _policies, _absences, _terms, _calendar, _currentUser);

    [Fact]
    public async Task Approving_a_month_with_breaches_without_acknowledging_is_refused()
    {
        WithAustrianPolicy();
        var sheet = SubmittedMonthWithABreach();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Handler().HandleAsync(new ApproveTimesheetCommand(sheet.Id, AcknowledgeBreaches: false)));

        Assert.Contains("must be acknowledged", ex.Message);
        Assert.Equal(TimesheetStatus.Submitted, sheet.Status);
        await _timesheets.DidNotReceive().UpdateAsync(sheet, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Acknowledging_the_breaches_approves_and_snapshots_them()
    {
        WithAustrianPolicy();
        var sheet = SubmittedMonthWithABreach();

        await Handler().HandleAsync(new ApproveTimesheetCommand(sheet.Id, AcknowledgeBreaches: true));

        Assert.Equal(TimesheetStatus.Approved, sheet.Status);

        var breach = Assert.Single(sheet.Breaches);
        Assert.Equal(BreachKind.DailyMaximum, breach.Kind);
        Assert.Equal(12m, breach.LimitValue);
        Assert.Equal(13m, breach.ActualValue);

        // Who accepted it, and when, is part of the record.
        Assert.Equal(ApproverId, breach.AcknowledgedById);
        Assert.NotNull(breach.AcknowledgedAt);
    }

    [Fact]
    public async Task A_compliant_month_needs_no_acknowledgement()
    {
        WithAustrianPolicy();
        var sheet = SubmittedCompliantMonth();

        await Handler().HandleAsync(new ApproveTimesheetCommand(sheet.Id, AcknowledgeBreaches: false));

        Assert.Equal(TimesheetStatus.Approved, sheet.Status);
        Assert.Empty(sheet.Breaches);
    }

    [Fact]
    public async Task With_no_policy_configured_nothing_is_flagged_and_approval_proceeds()
    {
        // A company that has not set up its rule set is not judged against invented limits.
        WithNoPolicy();
        var sheet = SubmittedMonthWithABreach();

        await Handler().HandleAsync(new ApproveTimesheetCommand(sheet.Id, AcknowledgeBreaches: false));

        Assert.Equal(TimesheetStatus.Approved, sheet.Status);
        Assert.Empty(sheet.Breaches);
    }

    [Fact]
    public async Task Reopening_an_approved_month_clears_its_acknowledged_breaches()
    {
        WithAustrianPolicy();
        var sheet = SubmittedMonthWithABreach();
        await Handler().HandleAsync(new ApproveTimesheetCommand(sheet.Id, AcknowledgeBreaches: true));
        Assert.Single(sheet.Breaches);

        sheet.Reopen(ApproverId);

        // They belonged to the withdrawn approval; the open month is evaluated live again.
        Assert.Empty(sheet.Breaches);
        Assert.Equal(TimesheetStatus.Draft, sheet.Status);
    }
}
