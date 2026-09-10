using NSubstitute;
using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Commands.ApproveAbsence;
using Proposly.Application.WorkTimeManagement.Commands.CancelAbsence;
using Proposly.Application.WorkTimeManagement.Commands.RequestAbsence;
using Proposly.Application.WorkTimeManagement.Queries.PreviewAbsence;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.CompanyManagement.Enums;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Services;

namespace Proposly.Application.Tests.WorkTimeManagement;

public class AbsenceHandlerTests
{
    private readonly IAbsenceRepository _absences = Substitute.For<IAbsenceRepository>();
    private readonly ICompanyRepository _companies = Substitute.For<ICompanyRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly TimeProvider _clock = new FakeClock(new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc));

    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ApproverId = Guid.NewGuid();

    // Mon 6 to Fri 17 July 2026 — 10 working days, still in the future on the fake clock.
    private static readonly DateOnly Start = new(2026, 7, 6);
    private static readonly DateOnly End = new(2026, 7, 17);

    public AbsenceHandlerTests()
    {
        _currentUser.CompanyId.Returns(CompanyId);
        _currentUser.UserId.Returns(UserId);
        _currentUser.CanViewAllEmployees.Returns(false);

        var company = Company.Create(CompanyId, "Test GmbH");
        company.SetPlan(PlanTier.Pro, null, null, null);
        _companies.GetByIdAsync(CompanyId).Returns(company);

        _absences.GetOverlappingAsync(
            Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);
    }

    private AbsenceEntitlement WithEntitlement(decimal entitled = 25m, decimal used = 0m)
    {
        var entitlement = AbsenceEntitlement.Create(CompanyId, UserId, 2026, entitled);
        if (used > 0) entitlement.Consume(used);
        _absences.GetEntitlementAsync(UserId, 2026).Returns(entitlement);
        return entitlement;
    }

    private RequestAbsenceCommandHandler RequestHandler()
        => new(_absences, _companies, _currentUser);

    private static RequestAbsenceCommand Request(
        AbsenceType type = AbsenceType.Vacation, bool firstHalf = false, bool lastHalf = false)
        => new(type, Start, End, firstHalf, lastHalf);

    private static AbsenceRequest Existing(
        AbsenceType type = AbsenceType.Vacation, Guid? userId = null)
        => AbsenceRequest.Create(
            CompanyId, userId ?? UserId, type, Start, End, false, false,
            WorkingDayCalculator.ConsumedDays(Start, End, false, false));

    // --- Requesting ---

    [Fact]
    public async Task A_vacation_request_snapshots_the_working_days_it_consumes()
    {
        WithEntitlement();

        await RequestHandler().HandleAsync(Request());

        await _absences.Received(1).AddAsync(
            Arg.Is<AbsenceRequest>(a => a.ConsumedDays == 10m
                                     && a.Status == AbsenceStatus.Pending
                                     && a.UserId == UserId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Half_days_reduce_the_consumed_total()
    {
        WithEntitlement();

        await RequestHandler().HandleAsync(Request(lastHalf: true));

        await _absences.Received(1).AddAsync(
            Arg.Is<AbsenceRequest>(a => a.ConsumedDays == 9.5m), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_overlapping_request_is_refused()
    {
        WithEntitlement();
        _absences.GetOverlappingAsync(UserId, Start, End, Arg.Any<CancellationToken>())
            .Returns([Existing()]);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => RequestHandler().HandleAsync(Request()));

        Assert.Contains("overlaps an existing", ex.Message);
        await _absences.DidNotReceive().AddAsync(Arg.Any<AbsenceRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_vacation_request_beyond_the_remaining_balance_is_refused()
    {
        WithEntitlement(entitled: 25m, used: 20m); // 5 remain, 10 requested

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => RequestHandler().HandleAsync(Request()));

        Assert.Contains("Only 5 vacation day(s) remain", ex.Message);
    }

    [Fact]
    public async Task Vacation_without_any_entitlement_set_is_refused_with_a_clear_message()
    {
        _absences.GetEntitlementAsync(UserId, 2026).Returns((AbsenceEntitlement?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => RequestHandler().HandleAsync(Request()));

        Assert.Contains("No vacation entitlement is set", ex.Message);
    }

    [Theory]
    [InlineData(AbsenceType.SickLeave)]
    [InlineData(AbsenceType.UnpaidLeave)]
    [InlineData(AbsenceType.ParentalLeave)]
    [InlineData(AbsenceType.SpecialLeave)]
    public async Task Non_vacation_absence_needs_no_entitlement(AbsenceType type)
    {
        // No entitlement configured at all — these types do not draw on it.
        _absences.GetEntitlementAsync(UserId, 2026).Returns((AbsenceEntitlement?)null);

        await RequestHandler().HandleAsync(Request(type));

        await _absences.Received(1).AddAsync(
            Arg.Is<AbsenceRequest>(a => a.Type == type), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_range_with_no_working_days_is_refused()
    {
        WithEntitlement();
        var weekend = new RequestAbsenceCommand(
            AbsenceType.Vacation, new DateOnly(2026, 7, 11), new DateOnly(2026, 7, 12));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => RequestHandler().HandleAsync(weekend));

        Assert.Contains("no working days", ex.Message);
    }

    [Fact]
    public async Task Requesting_is_refused_below_the_pro_plan()
    {
        var company = Company.Create(CompanyId, "Test GmbH");
        company.SetPlan(PlanTier.Starter, null, null, null);
        _companies.GetByIdAsync(CompanyId).Returns(company);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => RequestHandler().HandleAsync(Request()));
    }

    // --- Approving ---

    [Fact]
    public async Task Approving_vacation_draws_down_the_entitlement()
    {
        var entitlement = WithEntitlement();
        var absence = Existing();
        _absences.GetByIdAsync(absence.Id).Returns(absence);
        _currentUser.UserId.Returns(ApproverId);

        await new ApproveAbsenceCommandHandler(_absences, _currentUser)
            .HandleAsync(new ApproveAbsenceCommand(absence.Id));

        Assert.Equal(AbsenceStatus.Approved, absence.Status);
        Assert.Equal(10m, entitlement.UsedDays);
        Assert.Equal(15m, entitlement.RemainingDays);
    }

    [Fact]
    public async Task Approving_sick_leave_leaves_the_entitlement_untouched()
    {
        var entitlement = WithEntitlement();
        var absence = Existing(AbsenceType.SickLeave);
        _absences.GetByIdAsync(absence.Id).Returns(absence);
        _currentUser.UserId.Returns(ApproverId);

        await new ApproveAbsenceCommandHandler(_absences, _currentUser)
            .HandleAsync(new ApproveAbsenceCommand(absence.Id));

        Assert.Equal(AbsenceStatus.Approved, absence.Status);
        Assert.Equal(0m, entitlement.UsedDays);
    }

    [Fact]
    public async Task Approving_an_absence_the_caller_cannot_see_is_refused()
    {
        // A member's query filter hides other employees' rows, so the lookup returns null.
        _absences.GetByIdAsync(Arg.Any<Guid>()).Returns((AbsenceRequest?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new ApproveAbsenceCommandHandler(_absences, _currentUser)
                .HandleAsync(new ApproveAbsenceCommand(Guid.NewGuid())));
    }

    // --- Cancelling ---

    [Fact]
    public async Task Cancelling_an_approved_future_vacation_returns_the_days()
    {
        var entitlement = WithEntitlement();
        var absence = Existing();
        absence.Approve(ApproverId);
        entitlement.Consume(absence.ConsumedDays);
        _absences.GetByIdAsync(absence.Id).Returns(absence);

        await new CancelAbsenceCommandHandler(_absences, _clock)
            .HandleAsync(new CancelAbsenceCommand(absence.Id));

        Assert.Equal(AbsenceStatus.Cancelled, absence.Status);
        Assert.Equal(0m, entitlement.UsedDays);
    }

    [Fact]
    public async Task Cancelling_a_pending_request_returns_nothing_because_nothing_was_taken()
    {
        var entitlement = WithEntitlement();
        var absence = Existing();
        _absences.GetByIdAsync(absence.Id).Returns(absence);

        await new CancelAbsenceCommandHandler(_absences, _clock)
            .HandleAsync(new CancelAbsenceCommand(absence.Id));

        Assert.Equal(AbsenceStatus.Cancelled, absence.Status);
        Assert.Equal(0m, entitlement.UsedDays);
    }

    // --- Preview ---

    [Fact]
    public async Task The_preview_reports_the_cost_the_resulting_balance_and_the_excluded_days()
    {
        WithEntitlement(entitled: 25m, used: 5m);

        var result = await new PreviewAbsenceQueryHandler(_absences, _currentUser)
            .HandleAsync(new PreviewAbsenceQuery(AbsenceType.Vacation, Start, End));

        Assert.Equal(10m, result.ConsumedDays);
        Assert.Equal(10m, result.RemainingDaysAfter);   // 25 - 5 - 10
        Assert.False(result.ExceedsEntitlement);
        Assert.False(result.OverlapsExisting);
        Assert.Equal(2, result.NonWorkingDatesExcluded.Count); // one weekend
    }

    [Fact]
    public async Task The_preview_flags_a_request_that_would_exceed_the_balance()
    {
        WithEntitlement(entitled: 25m, used: 20m);

        var result = await new PreviewAbsenceQueryHandler(_absences, _currentUser)
            .HandleAsync(new PreviewAbsenceQuery(AbsenceType.Vacation, Start, End));

        Assert.True(result.ExceedsEntitlement);
        Assert.Equal(-5m, result.RemainingDaysAfter);
    }

    [Fact]
    public async Task The_preview_flags_an_overlap()
    {
        WithEntitlement();
        _absences.GetOverlappingAsync(UserId, Start, End, Arg.Any<CancellationToken>())
            .Returns([Existing()]);

        var result = await new PreviewAbsenceQueryHandler(_absences, _currentUser)
            .HandleAsync(new PreviewAbsenceQuery(AbsenceType.Vacation, Start, End));

        Assert.True(result.OverlapsExisting);
    }

    private sealed class FakeClock(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }
}
