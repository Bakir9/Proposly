using NSubstitute;
using Proposly.Application.Abstractions;
using Proposly.Application.Dashboard.Queries.GetWhosOut;
using Proposly.Application.Dashboard.Services;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.Tests.Dashboard;

public sealed class GetWhosOutQueryHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IWhosOutReader _reader = Substitute.For<IWhosOutReader>();
    private readonly INonWorkingDayRepository _holidays = Substitute.For<INonWorkingDayRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly GetWhosOutQueryHandler _sut;

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly User _me;
    private readonly User _colleague;

    public GetWhosOutQueryHandlerTests()
    {
        _me = User.Create(_companyId, "me@test.com", "h", "Me", "Tester");
        _colleague = User.Create(_companyId, "col@test.com", "h", "Col", "League");
        _currentUser.CompanyId.Returns(_companyId);
        _currentUser.UserId.Returns(_me.Id);
        _users.GetActiveAsync(Arg.Any<CancellationToken>()).Returns([_me, _colleague]);
        _reader.GetWorkPatternsAsync(_companyId, Arg.Any<CancellationToken>()).Returns([]);
        _holidays.GetForRangeAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _sut = new GetWhosOutQueryHandler(_users, _reader, _holidays, _currentUser);
    }

    private static DateOnly NextWorkday(DateOnly d)
        => d.DayOfWeek switch
        {
            DayOfWeek.Saturday => d.AddDays(2),
            DayOfWeek.Sunday => d.AddDays(1),
            _ => d,
        };

    [Fact]
    public async Task Sick_leave_type_is_redacted_for_viewers_without_the_admin_view()
    {
        var today = NextWorkday(DateOnly.FromDateTime(DateTime.Now));
        _currentUser.CanViewAllEmployees.Returns(false);
        _reader.GetApprovedAbsencesAsync(_companyId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([new AbsenceSlice(_colleague.Id, AbsenceType.SickLeave, today, today.AddDays(2))]);

        var result = await _sut.HandleAsync(new GetWhosOutQuery("month"));

        var person = Assert.Single(result.People);
        Assert.Equal("Absent", person.Type);
        Assert.False(result.CanSeeTypes);
    }

    [Fact]
    public async Task Admins_see_the_absence_type()
    {
        var today = NextWorkday(DateOnly.FromDateTime(DateTime.Now));
        _currentUser.CanViewAllEmployees.Returns(true);
        _reader.GetApprovedAbsencesAsync(_companyId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([new AbsenceSlice(_colleague.Id, AbsenceType.SickLeave, today, today.AddDays(2))]);

        var result = await _sut.HandleAsync(new GetWhosOutQuery("month"));

        Assert.Equal("Sick", Assert.Single(result.People).Type);
        Assert.True(result.CanSeeTypes);
    }

    [Fact]
    public async Task Public_holidays_count_nobody_as_out()
    {
        var today = NextWorkday(DateOnly.FromDateTime(DateTime.Now));
        _holidays.GetForRangeAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([NonWorkingDay.Create(_companyId, today, "Holiday", NonWorkingDayKind.PublicHoliday)]);
        _reader.GetApprovedAbsencesAsync(_companyId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([new AbsenceSlice(_colleague.Id, AbsenceType.Vacation, today, today)]);

        var result = await _sut.HandleAsync(new GetWhosOutQuery("today"));

        Assert.Equal(0, result.OutToday);
        Assert.Equal(0, result.InToday); // company holiday: nobody is scheduled either
    }

    [Fact]
    public async Task In_count_is_scheduled_employees_minus_out()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            // Weekend: everyone unscheduled, nobody in or out — still a valid assertion.
            var weekend = await _sut.HandleAsync(new GetWhosOutQuery("today"));
            Assert.Equal(0, weekend.InToday + weekend.OutToday);
            return;
        }

        _currentUser.CanViewAllEmployees.Returns(true);
        _reader.GetApprovedAbsencesAsync(_companyId, Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([new AbsenceSlice(_colleague.Id, AbsenceType.Vacation, today, today)]);

        var result = await _sut.HandleAsync(new GetWhosOutQuery("today"));

        Assert.Equal(2, result.TeamTotal);
        Assert.Equal(1, result.OutToday);
        Assert.Equal(1, result.InToday);
    }
}
