using NSubstitute;
using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Commands.CreateEmploymentTerms;
using Proposly.Application.WorkTimeManagement.Commands.CreateNonWorkingDay;
using Proposly.Application.WorkTimeManagement.Commands.DeleteNonWorkingDay;
using Proposly.Application.WorkTimeManagement.Commands.UpdateNonWorkingDay;
using Proposly.Application.WorkTimeManagement.Queries.GetTargetHours;
using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.Tests.WorkTimeManagement;

public class WorkCalendarHandlerTests
{
    private readonly INonWorkingDayRepository _calendar = Substitute.For<INonWorkingDayRepository>();
    private readonly IEmploymentTermsRepository _terms = Substitute.For<IEmploymentTermsRepository>();
    private readonly ITimesheetRepository _timesheets = Substitute.For<ITimesheetRepository>();
    private readonly IAbsenceRepository _absences = Substitute.For<IAbsenceRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly MayDay = new(2026, 5, 1);

    public WorkCalendarHandlerTests()
    {
        _currentUser.CompanyId.Returns(CompanyId);
        _currentUser.UserId.Returns(UserId);
        _currentUser.CanViewAllEmployees.Returns(true);

        _calendar.GetByDateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((NonWorkingDay?)null);

        _timesheets.GetForMonthAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        _absences.GetApprovedCoveringDateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);
    }

    private static NonWorkingDay Holiday(DateOnly? date = null)
        => NonWorkingDay.Create(
            CompanyId, date ?? MayDay, "Staatsfeiertag", NonWorkingDayKind.PublicHoliday);

    private static Timesheet ApprovedMay()
    {
        var sheet = Timesheet.Create(CompanyId, UserId, 2026, 5);
        sheet.AddOrUpdateDay(new DateOnly(2026, 5, 4), new TimeOnly(8, 0), new TimeOnly(16, 30), 30);
        sheet.Submit();
        sheet.Approve(Guid.NewGuid());
        return sheet;
    }

    // --- Creating calendar entries ---

    [Fact]
    public async Task A_second_entry_on_the_same_date_is_refused()
    {
        _calendar.GetByDateAsync(MayDay, Arg.Any<CancellationToken>()).Returns(Holiday());
        var handler = new CreateNonWorkingDayCommandHandler(_calendar, _currentUser);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(new CreateNonWorkingDayCommand(
                MayDay, "Betriebsurlaub", NonWorkingDayKind.CompanyClosure)));

        Assert.Contains("already marked", ex.Message);
        await _calendar.DidNotReceive().AddAsync(Arg.Any<NonWorkingDay>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_holiday_is_created_with_no_vacation_cost()
    {
        var handler = new CreateNonWorkingDayCommandHandler(_calendar, _currentUser);

        await handler.HandleAsync(new CreateNonWorkingDayCommand(
            MayDay, "Staatsfeiertag", NonWorkingDayKind.PublicHoliday));

        await _calendar.Received(1).AddAsync(
            Arg.Is<NonWorkingDay>(d => !d.ConsumesVacation && d.Source == EntrySource.Manual),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_closure_is_created_consuming_vacation_by_default()
    {
        var handler = new CreateNonWorkingDayCommandHandler(_calendar, _currentUser);

        await handler.HandleAsync(new CreateNonWorkingDayCommand(
            MayDay, "Betriebsurlaub", NonWorkingDayKind.CompanyClosure));

        await _calendar.Received(1).AddAsync(
            Arg.Is<NonWorkingDay>(d => d.ConsumesVacation), Arg.Any<CancellationToken>());
    }

    // --- Guarding months that are already signed off ---

    [Fact]
    public async Task Editing_a_day_inside_an_approved_month_is_refused()
    {
        var day = Holiday();
        _calendar.GetByIdAsync(day.Id, Arg.Any<CancellationToken>()).Returns(day);
        _timesheets.GetForMonthAsync(2026, 5, Arg.Any<CancellationToken>()).Returns([ApprovedMay()]);

        var handler = new UpdateNonWorkingDayCommandHandler(_calendar, _timesheets);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(new UpdateNonWorkingDayCommand(
                day.Id, "Renamed", NonWorkingDayKind.PublicHoliday, false)));

        Assert.Contains("already had approved", ex.Message);
        Assert.Equal("Staatsfeiertag", day.Name);
    }

    [Fact]
    public async Task Deleting_a_day_inside_an_approved_month_is_refused()
    {
        var day = Holiday();
        _calendar.GetByIdAsync(day.Id, Arg.Any<CancellationToken>()).Returns(day);
        _timesheets.GetForMonthAsync(2026, 5, Arg.Any<CancellationToken>()).Returns([ApprovedMay()]);

        var handler = new DeleteNonWorkingDayCommandHandler(_calendar, _absences, _timesheets);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(new DeleteNonWorkingDayCommand(day.Id)));

        await _calendar.DidNotReceive().RemoveAsync(Arg.Any<NonWorkingDay>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Editing_a_day_in_an_open_month_is_allowed()
    {
        var day = Holiday();
        _calendar.GetByIdAsync(day.Id, Arg.Any<CancellationToken>()).Returns(day);

        var handler = new UpdateNonWorkingDayCommandHandler(_calendar, _timesheets);

        await handler.HandleAsync(new UpdateNonWorkingDayCommand(
            day.Id, "Corrected", NonWorkingDayKind.PublicHoliday, false));

        Assert.Equal("Corrected", day.Name);
    }

    [Fact]
    public async Task Deleting_a_day_covered_by_an_approved_absence_is_refused()
    {
        // Removing the holiday would make the day chargeable again, leaving the approved absence
        // short by a day. Refused rather than silently adjusting someone's entitlement.
        var day = Holiday();
        _calendar.GetByIdAsync(day.Id, Arg.Any<CancellationToken>()).Returns(day);

        var absence = AbsenceRequest.Create(
            CompanyId, UserId, AbsenceType.Vacation, MayDay, MayDay, false, false, 1m);
        absence.Approve(Guid.NewGuid());

        _absences.GetApprovedCoveringDateAsync(MayDay, Arg.Any<CancellationToken>())
            .Returns([absence]);

        var handler = new DeleteNonWorkingDayCommandHandler(_calendar, _absences, _timesheets);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(new DeleteNonWorkingDayCommand(day.Id)));

        Assert.Contains("approved absence", ex.Message);
        await _calendar.DidNotReceive().RemoveAsync(Arg.Any<NonWorkingDay>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deleting_an_unaffected_day_succeeds()
    {
        var day = Holiday();
        _calendar.GetByIdAsync(day.Id, Arg.Any<CancellationToken>()).Returns(day);

        var handler = new DeleteNonWorkingDayCommandHandler(_calendar, _absences, _timesheets);

        await handler.HandleAsync(new DeleteNonWorkingDayCommand(day.Id));

        await _calendar.Received(1).RemoveAsync(day, Arg.Any<CancellationToken>());
    }

    // --- Employment terms ---

    [Fact]
    public async Task New_terms_close_the_previous_version()
    {
        var january = EmploymentTerms.Create(
            CompanyId, UserId, new DateOnly(2026, 1, 1), 38.5m, WeekDays.MondayToFriday, 25m);

        _terms.GetLatestAsync(UserId, Arg.Any<CancellationToken>()).Returns(january);

        var handler = new CreateEmploymentTermsCommandHandler(_terms, _currentUser);

        await handler.HandleAsync(new CreateEmploymentTermsCommand(
            UserId, new DateOnly(2026, 7, 1), 30m, WeekDays.MondayToFriday, 25m));

        Assert.Equal(new DateOnly(2026, 6, 30), january.ValidTo);
        await _terms.Received(1).AddAsync(
            Arg.Is<EmploymentTerms>(t => t.WeeklyHours == 30m), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Terms_starting_before_the_current_version_are_refused()
    {
        var july = EmploymentTerms.Create(
            CompanyId, UserId, new DateOnly(2026, 7, 1), 30m, WeekDays.MondayToFriday, 25m);

        _terms.GetLatestAsync(UserId, Arg.Any<CancellationToken>()).Returns(july);

        var handler = new CreateEmploymentTermsCommandHandler(_terms, _currentUser);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(new CreateEmploymentTermsCommand(
                UserId, new DateOnly(2026, 1, 1), 38.5m, WeekDays.MondayToFriday, 25m)));

        Assert.Contains("must take effect after", ex.Message);
        await _terms.DidNotReceive().AddAsync(Arg.Any<EmploymentTerms>(), Arg.Any<CancellationToken>());
    }

    // --- Target hours ---

    [Fact]
    public async Task Target_hours_report_as_unavailable_when_no_terms_cover_the_month()
    {
        _terms.GetForRangeAsync(UserId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _calendar.GetForRangeAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _absences.GetApprovedInRangeAsync(
            UserId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetTargetHoursQueryHandler(_terms, _calendar, _absences, _currentUser);

        var result = await handler.HandleAsync(new GetTargetHoursQuery(2026, 3));

        Assert.Null(result.TargetHours);
        Assert.Equal(0, result.WorkingDays);
    }

    [Fact]
    public async Task Target_hours_net_off_holidays_and_approved_absence()
    {
        var terms = EmploymentTerms.Create(
            CompanyId, UserId, new DateOnly(2026, 1, 1), 38.5m, WeekDays.MondayToFriday, 25m);

        var holiday = NonWorkingDay.Create(
            CompanyId, new DateOnly(2026, 3, 2), "Holiday", NonWorkingDayKind.PublicHoliday);

        var leave = AbsenceRequest.Create(
            CompanyId, UserId, AbsenceType.Vacation,
            new DateOnly(2026, 3, 3), new DateOnly(2026, 3, 4), false, false, 2m);
        leave.Approve(Guid.NewGuid());

        _terms.GetForRangeAsync(UserId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([terms]);
        _calendar.GetForRangeAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([holiday]);
        _absences.GetApprovedInRangeAsync(
            UserId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([leave]);

        var handler = new GetTargetHoursQueryHandler(_terms, _calendar, _absences, _currentUser);

        var result = await handler.HandleAsync(new GetTargetHoursQuery(2026, 3));

        // 22 weekdays, less 1 holiday = 21 expected working days; less 2 days of leave = 19 at
        // 7.7 hours = 146.3.
        Assert.Equal(21, result.WorkingDays);
        Assert.Equal(1, result.NonWorkingDaysExcluded);
        Assert.Equal(146.3m, result.TargetHours);
    }
}
