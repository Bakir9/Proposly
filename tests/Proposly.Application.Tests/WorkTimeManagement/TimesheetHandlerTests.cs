using NSubstitute;
using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Commands.ApproveTimesheet;
using Proposly.Application.WorkTimeManagement.Commands.SubmitTimesheet;
using Proposly.Application.WorkTimeManagement.Commands.UpsertWorkDay;
using Proposly.Application.WorkTimeManagement.Queries.GetMyTimesheet;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.CompanyManagement.Enums;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.Tests.WorkTimeManagement;

public class TimesheetHandlerTests
{
    private readonly ITimesheetRepository _timesheets = Substitute.For<ITimesheetRepository>();
    private readonly ICompanyRepository _companies = Substitute.For<ICompanyRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IWorkTimePolicyRepository _policies = Substitute.For<IWorkTimePolicyRepository>();
    private readonly IAbsenceRepository _absences = Substitute.For<IAbsenceRepository>();
    private readonly IEmploymentTermsRepository _terms = Substitute.For<IEmploymentTermsRepository>();
    private readonly INonWorkingDayRepository _calendar = Substitute.For<INonWorkingDayRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ApproverId = Guid.NewGuid();

    public TimesheetHandlerTests()
    {
        _currentUser.CompanyId.Returns(CompanyId);
        _currentUser.UserId.Returns(UserId);
        _currentUser.Role.Returns(nameof(UserRole.Member));
        _currentUser.CanViewAllEmployees.Returns(false);

        _users.GetByIdAsync(UserId).Returns(
            User.Create(CompanyId, "b@kost.at", "hash", "Bakir", "Malkoc", UserRole.Member));
    }

    private void OnPlan(PlanTier tier, DateTime? expiresAt = null)
    {
        var company = Company.Create(CompanyId, "Test GmbH");
        company.SetPlan(tier, null, null, expiresAt);
        _companies.GetByIdAsync(CompanyId).Returns(company);
    }

    private static Timesheet March(Guid userId)
        => Timesheet.Create(CompanyId, userId, 2026, 3);

    private static UpsertWorkDayCommand UpsertMarch2 => new(
        2026, 3, new DateOnly(2026, 3, 2), new TimeOnly(8, 30), new TimeOnly(17, 0), 30);

    // --- Plan gate ---

    [Theory]
    [InlineData(PlanTier.Free)]
    [InlineData(PlanTier.Starter)]
    public async Task Recording_time_is_refused_below_pro(PlanTier tier)
    {
        OnPlan(tier);
        var handler = new UpsertWorkDayCommandHandler(_timesheets, _companies, _currentUser);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(UpsertMarch2));

        Assert.Contains("Pro and Business", ex.Message);
        await _timesheets.DidNotReceive().AddAsync(Arg.Any<Timesheet>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Recording_time_is_refused_when_the_plan_has_expired()
    {
        OnPlan(PlanTier.Pro, expiresAt: DateTime.UtcNow.AddDays(-1));
        var handler = new UpsertWorkDayCommandHandler(_timesheets, _companies, _currentUser);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(UpsertMarch2));
    }

    // --- Recording ---

    [Fact]
    public async Task Recording_the_first_day_of_a_month_creates_the_timesheet()
    {
        OnPlan(PlanTier.Pro);
        _timesheets.GetForUserAsync(UserId, 2026, 3).Returns((Timesheet?)null);
        var handler = new UpsertWorkDayCommandHandler(_timesheets, _companies, _currentUser);

        await handler.HandleAsync(UpsertMarch2);

        await _timesheets.Received(1).AddAsync(
            Arg.Is<Timesheet>(t => t.UserId == UserId
                                && t.Year == 2026 && t.Month == 3
                                && t.TotalWorkedHours == 8.0m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Recording_a_day_in_an_existing_month_updates_rather_than_inserts()
    {
        OnPlan(PlanTier.Pro);
        var sheet = March(UserId);
        _timesheets.GetForUserAsync(UserId, 2026, 3).Returns(sheet);
        var handler = new UpsertWorkDayCommandHandler(_timesheets, _companies, _currentUser);

        await handler.HandleAsync(UpsertMarch2);

        Assert.Equal(8.0m, sheet.TotalWorkedHours);
        await _timesheets.Received(1).UpdateAsync(sheet, Arg.Any<CancellationToken>());
        await _timesheets.DidNotReceive().AddAsync(Arg.Any<Timesheet>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submitting_a_month_that_was_never_recorded_is_refused()
    {
        _timesheets.GetForUserAsync(UserId, 2026, 3).Returns((Timesheet?)null);
        var handler = new SubmitTimesheetCommandHandler(_timesheets, _currentUser);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(new SubmitTimesheetCommand(2026, 3)));
    }

    // --- Reading ---

    [Fact]
    public async Task An_untouched_month_reads_as_an_empty_draft_and_is_not_persisted()
    {
        _timesheets.GetForUserAsync(UserId, 2026, 3).Returns((Timesheet?)null);
        var handler = new GetMyTimesheetQueryHandler(_timesheets, _policies, _absences, _users, _currentUser);

        var result = await handler.HandleAsync(new GetMyTimesheetQuery(2026, 3));

        Assert.NotNull(result);
        Assert.Null(result!.Id);
        Assert.Equal(TimesheetStatus.Draft, result.Status);
        Assert.Empty(result.Days);
        Assert.Equal(0m, result.TotalWorkedHours);

        // Reading must not create a row.
        await _timesheets.DidNotReceive().AddAsync(Arg.Any<Timesheet>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_recorded_month_reads_back_its_days_in_date_order()
    {
        var sheet = March(UserId);
        sheet.AddOrUpdateDay(new DateOnly(2026, 3, 20), new TimeOnly(8, 0), new TimeOnly(16, 0), 30);
        sheet.AddOrUpdateDay(new DateOnly(2026, 3, 2), new TimeOnly(8, 0), new TimeOnly(16, 0), 30);
        _timesheets.GetForUserAsync(UserId, 2026, 3).Returns(sheet);
        var handler = new GetMyTimesheetQueryHandler(_timesheets, _policies, _absences, _users, _currentUser);

        var result = await handler.HandleAsync(new GetMyTimesheetQuery(2026, 3));

        Assert.Equal(2, result!.Days.Count);
        Assert.Equal(new DateOnly(2026, 3, 2), result.Days[0].Date);
        Assert.Equal("Bakir Malkoc", result.EmployeeName);
    }

    // --- Approval ---

    [Fact]
    public async Task Approving_records_the_approver_and_is_not_self_approval()
    {
        var sheet = March(UserId);
        sheet.AddOrUpdateDay(new DateOnly(2026, 3, 2), new TimeOnly(8, 0), new TimeOnly(16, 0), 30);
        sheet.Submit();
        _timesheets.GetByIdAsync(sheet.Id).Returns(sheet);

        _currentUser.UserId.Returns(ApproverId);
        _currentUser.CanViewAllEmployees.Returns(true);
        var handler = new ApproveTimesheetCommandHandler(
            _timesheets, _policies, _absences, _terms, _calendar, _currentUser);

        await handler.HandleAsync(new ApproveTimesheetCommand(sheet.Id));

        Assert.Equal(TimesheetStatus.Approved, sheet.Status);
        Assert.Equal(ApproverId, sheet.ApprovedById);
        Assert.False(sheet.IsSelfApproved);
    }

    [Fact]
    public async Task An_owner_approving_their_own_month_is_flagged_as_self_approved()
    {
        var sheet = March(ApproverId);
        sheet.AddOrUpdateDay(new DateOnly(2026, 3, 2), new TimeOnly(8, 0), new TimeOnly(16, 0), 30);
        sheet.Submit();
        _timesheets.GetByIdAsync(sheet.Id).Returns(sheet);

        _currentUser.UserId.Returns(ApproverId);
        _currentUser.CanViewAllEmployees.Returns(true);
        var handler = new ApproveTimesheetCommandHandler(
            _timesheets, _policies, _absences, _terms, _calendar, _currentUser);

        await handler.HandleAsync(new ApproveTimesheetCommand(sheet.Id));

        Assert.True(sheet.IsSelfApproved);
    }

    [Fact]
    public async Task Approving_a_timesheet_the_caller_cannot_see_is_refused()
    {
        // A member's query filter hides other employees' rows, so the lookup returns null and the
        // handler reports not found rather than leaking that the row exists.
        _timesheets.GetByIdAsync(Arg.Any<Guid>()).Returns((Timesheet?)null);
        var handler = new ApproveTimesheetCommandHandler(
            _timesheets, _policies, _absences, _terms, _calendar, _currentUser);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(new ApproveTimesheetCommand(Guid.NewGuid())));

        Assert.Contains("not found", ex.Message);
    }
}
