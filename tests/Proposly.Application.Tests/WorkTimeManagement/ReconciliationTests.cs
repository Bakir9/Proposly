using NSubstitute;
using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Queries.GetReconciliation;
using Proposly.Application.WorkTimeManagement.Services;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.CompanyManagement.Enums;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.Tests.WorkTimeManagement;

/// <summary>
/// Recorded working hours against hours booked to projects. The view reports; it never
/// reconciles, and it never writes.
/// </summary>
public class ReconciliationTests
{
    private readonly ITimesheetRepository _timesheets = Substitute.For<ITimesheetRepository>();
    private readonly IProjectBookingReader _bookings = Substitute.For<IProjectBookingReader>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProjectA = Guid.NewGuid();
    private static readonly Guid ProjectB = Guid.NewGuid();

    public ReconciliationTests()
    {
        _currentUser.CompanyId.Returns(CompanyId);
        _currentUser.UserId.Returns(UserId);
        _currentUser.CanViewAllEmployees.Returns(true);

        _users.GetByIdAsync(UserId).Returns(
            User.Create(CompanyId, "b@kost.at", "hash", "Bakir", "Malkoc", UserRole.Member));
    }

    /// <summary>A March timesheet with the given number of eight-hour days recorded.</summary>
    private Timesheet MarchWith(int days)
    {
        var sheet = Timesheet.Create(CompanyId, UserId, 2026, 3);

        for (var i = 0; i < days; i++)
        {
            sheet.AddOrUpdateDay(
                new DateOnly(2026, 3, 2).AddDays(i),
                new TimeOnly(8, 0), new TimeOnly(16, 30), 30);
        }

        _timesheets.GetForUserAsync(UserId, 2026, 3).Returns(sheet);
        return sheet;
    }

    private void Booked(decimal projectA, decimal projectB = 0m, decimal unattributed = 0m)
    {
        var byProject = new List<ProjectBookedHours> { new(ProjectA, "Website relaunch", projectA) };
        if (projectB > 0m) byProject.Add(new ProjectBookedHours(ProjectB, "CRM migration", projectB));

        _bookings.GetBookedHoursAsync(
            UserId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new ProjectBookingSummary(byProject, unattributed));
    }

    private GetReconciliationQueryHandler Handler()
        => new(_timesheets, _bookings, _users, _currentUser);

    [Fact]
    public async Task Unbooked_hours_are_the_difference_between_recorded_and_booked()
    {
        MarchWith(21);              // 168 recorded
        Booked(96m, 45m);           // 141 booked

        var result = await Handler().HandleAsync(new GetReconciliationQuery(2026, 3));

        Assert.Equal(168m, result!.RecordedWorkingHours);
        Assert.Equal(141m, result.ProjectBookedHours);
        Assert.Equal(27m, result.UnbookedHours);
        Assert.False(result.OverBooked);
    }

    [Fact]
    public async Task Hours_are_summed_across_every_project()
    {
        MarchWith(21);
        Booked(96m, 45m);

        var result = await Handler().HandleAsync(new GetReconciliationQuery(2026, 3));

        Assert.Equal(2, result!.ByProject.Count);
        Assert.Equal(141m, result.ByProject.Sum(p => p.BookedHours));
    }

    [Fact]
    public async Task Booking_more_than_was_recorded_is_flagged_rather_than_errored()
    {
        // Usually means a day was booked to a project but never recorded as working time.
        MarchWith(10);              // 80 recorded
        Booked(100m);

        var result = await Handler().HandleAsync(new GetReconciliationQuery(2026, 3));

        Assert.True(result!.OverBooked);
        Assert.Equal(0m, result.UnbookedHours);   // never negative
        Assert.Equal(100m, result.ProjectBookedHours);
    }

    [Fact]
    public async Task With_nothing_booked_every_recorded_hour_is_unbooked()
    {
        MarchWith(21);
        Booked(0m);

        var result = await Handler().HandleAsync(new GetReconciliationQuery(2026, 3));

        Assert.Equal(168m, result!.UnbookedHours);
    }

    [Fact]
    public async Task Unresolvable_memberships_are_reported_rather_than_dropped()
    {
        MarchWith(21);
        Booked(100m, unattributed: 12m);

        var result = await Handler().HandleAsync(new GetReconciliationQuery(2026, 3));

        Assert.Equal(12m, result!.UnattributedBookedHours);
    }

    [Fact]
    public async Task A_month_with_no_timesheet_has_nothing_to_reconcile()
    {
        _timesheets.GetForUserAsync(UserId, 2026, 3).Returns((Timesheet?)null);

        Assert.Null(await Handler().HandleAsync(new GetReconciliationQuery(2026, 3)));
    }

    [Fact]
    public async Task The_view_never_writes_to_a_timesheet_or_a_project()
    {
        // The regression that matters: this must stay a projection. Nothing it does may touch
        // project costs or profitability, which the module is forbidden from altering.
        MarchWith(21);
        Booked(141m);

        await Handler().HandleAsync(new GetReconciliationQuery(2026, 3));

        await _timesheets.DidNotReceive().AddAsync(Arg.Any<Timesheet>(), Arg.Any<CancellationToken>());
        await _timesheets.DidNotReceive().UpdateAsync(Arg.Any<Timesheet>(), Arg.Any<CancellationToken>());

        // The booking reader is the only project-side call, and it is read-only by contract.
        await _bookings.Received(1).GetBookedHoursAsync(
            UserId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }
}
