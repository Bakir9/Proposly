using NSubstitute;
using Proposly.Application.Abstractions;
using Proposly.Application.Hr.Commands.UpdateWorker;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.HrManagement.Entities;
using Proposly.Domain.HrManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.Tests.Hr;

public sealed class UpdateWorkerCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IEmployeeProfileRepository _profiles = Substitute.For<IEmployeeProfileRepository>();
    private readonly IEmploymentTermsRepository _terms = Substitute.For<IEmploymentTermsRepository>();
    private readonly IAbsenceRepository _absences = Substitute.For<IAbsenceRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly UpdateWorkerCommandHandler _sut;

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly User _user;
    private readonly EmployeeProfile _profile;

    public UpdateWorkerCommandHandlerTests()
    {
        _currentUser.CompanyId.Returns(_companyId);
        _user = User.CreateProfileOnly(_companyId, "Mujo", "Mujic");
        _profile = EmployeeProfile.Create(_companyId, _user.Id);
        _users.GetByIdAsync(_user.Id).Returns(_user);
        _profiles.GetByUserIdAsync(_user.Id).Returns(_profile);
        _sut = new UpdateWorkerCommandHandler(_users, _profiles, _terms, _absences, _currentUser);
    }

    private UpdateWorkerCommand Command(decimal?[] dayHours) => new(
        _user.Id, "Mujo", "Mujic",
        "Mason", "+43 1", null, null, null, null, "Construction", null, null,
        null, null, null, null, null, null,
        EmploymentType.FullTime, WorkModel.FullTime, dayHours,
        null, null, null, null,
        OvertimeMode.TimeOff, null, null, null, null,
        true, true, false, false, null, null, null, null,
        25, 0, []);

    private static readonly decimal?[] FiveDays = [7.7m, 7.7m, 7.7m, 7.7m, 7.7m, null, null];

    [Fact]
    public async Task HandleAsync_NoExistingTerms_CreatesFirstVersion()
    {
        _terms.GetLatestAsync(_user.Id).Returns((EmploymentTerms?)null);
        EmploymentTerms? created = null;
        await _terms.AddAsync(Arg.Do<EmploymentTerms>(t => created = t));

        await _sut.HandleAsync(Command(FiveDays));

        Assert.NotNull(created);
        Assert.Equal(38.5m, created!.WeeklyHours);
        Assert.Equal(WeekDays.MondayToFriday, created.WorkingDays);
        Assert.Equal("Mason", _profile.JobTitle);
    }

    [Fact]
    public async Task HandleAsync_UnchangedContract_CreatesNoNewVersion()
    {
        var existing = EmploymentTerms.Create(
            _companyId, _user.Id, new DateOnly(2026, 1, 1), 38.5m, WeekDays.MondayToFriday, 25,
            hr: new HrTerms(
                MondayHours: 7.7m, TuesdayHours: 7.7m, WednesdayHours: 7.7m,
                ThursdayHours: 7.7m, FridayHours: 7.7m));
        _terms.GetLatestAsync(_user.Id).Returns(existing);

        await _sut.HandleAsync(Command(FiveDays));

        await _terms.DidNotReceive().AddAsync(Arg.Any<EmploymentTerms>());
        await _terms.DidNotReceive().RemoveAsync(Arg.Any<EmploymentTerms>());
    }

    [Fact]
    public async Task HandleAsync_ChangedSchedule_ClosesOldAndCreatesNewVersion()
    {
        var existing = EmploymentTerms.Create(
            _companyId, _user.Id, new DateOnly(2026, 1, 1), 38.5m, WeekDays.MondayToFriday, 25,
            hr: new HrTerms(
                MondayHours: 7.7m, TuesdayHours: 7.7m, WednesdayHours: 7.7m,
                ThursdayHours: 7.7m, FridayHours: 7.7m));
        _terms.GetLatestAsync(_user.Id).Returns(existing);
        EmploymentTerms? created = null;
        await _terms.AddAsync(Arg.Do<EmploymentTerms>(t => created = t));

        decimal?[] fourDays = [8, 8, 8, 8, null, null, null];
        await _sut.HandleAsync(Command(fourDays));

        Assert.NotNull(existing.ValidTo);
        Assert.NotNull(created);
        Assert.Equal(32m, created!.WeeklyHours);
        Assert.Equal(existing.ValidTo!.Value.AddDays(1), created.ValidFrom);
        await _terms.DidNotReceive().RemoveAsync(Arg.Any<EmploymentTerms>());
    }

    [Fact]
    public async Task HandleAsync_SameDayChange_ReplacesTodaysVersion()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var existing = EmploymentTerms.Create(
            _companyId, _user.Id, today, 38.5m, WeekDays.MondayToFriday, 25);
        _terms.GetLatestAsync(_user.Id).Returns(existing);
        EmploymentTerms? created = null;
        await _terms.AddAsync(Arg.Do<EmploymentTerms>(t => created = t));

        await _sut.HandleAsync(Command(FiveDays));

        await _terms.Received(1).RemoveAsync(existing);
        Assert.NotNull(created);
        Assert.Equal(today, created!.ValidFrom);
    }

    [Fact]
    public async Task HandleAsync_UpsertsEntitlementForCurrentYear()
    {
        _terms.GetLatestAsync(_user.Id).Returns((EmploymentTerms?)null);
        _absences.GetEntitlementAsync(_user.Id, DateTime.UtcNow.Year).Returns((AbsenceEntitlement?)null);

        await _sut.HandleAsync(Command(FiveDays));

        await _absences.Received(1).AddEntitlementAsync(
            Arg.Is<AbsenceEntitlement>(e => e.UserId == _user.Id && e.EntitledDays == 25));
    }

    [Fact]
    public async Task HandleAsync_UnknownWorker_Throws()
    {
        _users.GetByIdAsync(Arg.Any<Guid>()).Returns((User?)null);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(Command(FiveDays)));
    }
}
