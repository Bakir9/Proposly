using NSubstitute;
using Proposly.Application.Abstractions;
using Proposly.Application.Hr.Commands.AddWorker;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.HrManagement.Entities;
using Proposly.Domain.HrManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.Tests.Hr;

public sealed class AddWorkerCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IEmployeeProfileRepository _profiles = Substitute.For<IEmployeeProfileRepository>();
    private readonly IEmploymentTermsRepository _terms = Substitute.For<IEmploymentTermsRepository>();
    private readonly IAbsenceRepository _absences = Substitute.For<IAbsenceRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly AddWorkerCommandHandler _sut;

    private readonly Guid _companyId = Guid.NewGuid();

    public AddWorkerCommandHandlerTests()
    {
        _currentUser.CompanyId.Returns(_companyId);
        _sut = new AddWorkerCommandHandler(_users, _profiles, _terms, _absences, _currentUser);
    }

    [Fact]
    public async Task HandleAsync_CreatesProfileOnlyUserProfileAndDefaultTerms()
    {
        User? savedUser = null;
        EmployeeProfile? savedProfile = null;
        EmploymentTerms? savedTerms = null;
        await _users.AddAsync(Arg.Do<User>(u => savedUser = u));
        await _profiles.AddAsync(Arg.Do<EmployeeProfile>(p => savedProfile = p));
        await _terms.AddAsync(Arg.Do<EmploymentTerms>(t => savedTerms = t));

        var userId = await _sut.HandleAsync(new AddWorkerCommand("Mujo", "Mujic", "Mason"));

        Assert.NotNull(savedUser);
        Assert.True(savedUser!.IsProfileOnly);
        Assert.Equal(userId, savedUser.Id);
        Assert.Equal(_companyId, savedUser.CompanyId);

        Assert.NotNull(savedProfile);
        Assert.Equal(userId, savedProfile!.UserId);
        Assert.Equal("Mason", savedProfile.JobTitle);

        Assert.NotNull(savedTerms);
        Assert.Equal(userId, savedTerms!.UserId);
        Assert.Equal(38.5m, savedTerms.WeeklyHours);
        Assert.Equal(WeekDays.MondayToFriday, savedTerms.WorkingDays);
        Assert.Equal(7.7m, savedTerms.MondayHours);

        // The entitlement row exists from day one — vacation approval consumes from it.
        await _absences.Received(1).AddEntitlementAsync(
            Arg.Is<AbsenceEntitlement>(e =>
                e.UserId == userId && e.Year == DateTime.UtcNow.Year && e.EntitledDays == 25));

        await _profiles.Received(1).SaveChangesAsync();
    }
}
