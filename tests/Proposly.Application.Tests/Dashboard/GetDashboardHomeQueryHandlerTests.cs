using NSubstitute;
using Proposly.Application.Abstractions;
using Proposly.Application.Dashboard.Queries.GetDashboardHome;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.HrManagement.Entities;
using Proposly.Domain.HrManagement.Repositories;
using Proposly.Domain.OfferManagement.Repositories;
using Proposly.Domain.ProjectManagement.Entities;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Repositories;
using Proposly.Shared.ValueObjects;

namespace Proposly.Application.Tests.Dashboard;

public sealed class GetDashboardHomeQueryHandlerTests
{
    private readonly IOfferRepository _offers = Substitute.For<IOfferRepository>();
    private readonly IClientRepository _clients = Substitute.For<IClientRepository>();
    private readonly IProjectRepository _projects = Substitute.For<IProjectRepository>();
    private readonly ITimesheetRepository _timesheets = Substitute.For<ITimesheetRepository>();
    private readonly IAbsenceRepository _absences = Substitute.For<IAbsenceRepository>();
    private readonly IEmploymentTermsRepository _terms = Substitute.For<IEmploymentTermsRepository>();
    private readonly IEmployeeProfileRepository _profiles = Substitute.For<IEmployeeProfileRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly GetDashboardHomeQueryHandler _sut;

    private readonly Guid _companyId = Guid.NewGuid();

    public GetDashboardHomeQueryHandlerTests()
    {
        _currentUser.CompanyId.Returns(_companyId);
        _currentUser.UserId.Returns(Guid.NewGuid());
        _currentUser.CanViewAllEmployees.Returns(true);
        _offers.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        _clients.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        _projects.GetAllForDashboardAsync(Arg.Any<CancellationToken>()).Returns([]);
        _timesheets.GetSubmittedAsync(Arg.Any<CancellationToken>()).Returns([]);
        _timesheets.GetDaysInRangeAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns([]);
        _absences.GetPendingAsync(Arg.Any<CancellationToken>()).Returns([]);
        _profiles.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        _users.GetActiveAsync(Arg.Any<CancellationToken>()).Returns([]);
        _sut = new GetDashboardHomeQueryHandler(
            _offers, _clients, _projects, _timesheets, _absences, _terms, _profiles, _users, _currentUser);
    }

    [Fact]
    public async Task Contract_ending_within_two_months_raises_an_attention_card()
    {
        var worker = User.CreateProfileOnly(_companyId, "Amar", "Mujic");
        var profile = EmployeeProfile.Create(_companyId, worker.Id);
        profile.UpdatePersonal(null, null, null, null, null, null, null, null, null, null, null, null,
            employmentStart: DateOnly.FromDateTime(DateTime.Now).AddYears(-1),
            employmentEnd: DateOnly.FromDateTime(DateTime.Now).AddDays(30));
        _profiles.GetAllAsync(Arg.Any<CancellationToken>()).Returns([profile]);
        _users.GetActiveAsync(Arg.Any<CancellationToken>()).Returns([worker]);

        var result = await _sut.HandleAsync(new GetDashboardHomeQuery());

        var card = Assert.Single(result.Attention, a => a.Kind == "contract_expiring");
        Assert.Contains("Amar Mujic", card.Title);
    }

    [Fact]
    public async Task Contract_ending_far_in_the_future_or_in_the_past_raises_nothing()
    {
        var worker = User.CreateProfileOnly(_companyId, "Hasan", "Hodzic");
        var far = EmployeeProfile.Create(_companyId, worker.Id);
        far.UpdatePersonal(null, null, null, null, null, null, null, null, null, null, null, null,
            null, DateOnly.FromDateTime(DateTime.Now).AddDays(200));
        var past = EmployeeProfile.Create(_companyId, Guid.NewGuid());
        past.UpdatePersonal(null, null, null, null, null, null, null, null, null, null, null, null,
            null, DateOnly.FromDateTime(DateTime.Now).AddDays(-5));
        _profiles.GetAllAsync(Arg.Any<CancellationToken>()).Returns([far, past]);

        var result = await _sut.HandleAsync(new GetDashboardHomeQuery());

        Assert.DoesNotContain(result.Attention, a => a.Kind == "contract_expiring");
    }

    [Fact]
    public async Task Overdue_tasks_raise_an_attention_card_linking_to_the_board()
    {
        var project = Project.Create(_companyId, "P", null, Guid.NewGuid(), "C",
            new Money(1000m, "EUR"), DateOnly.FromDateTime(DateTime.Now).AddDays(-30), null);
        project.AddTask("Late task", null, 8m,
            DateOnly.FromDateTime(DateTime.Now).AddDays(-10),
            DateOnly.FromDateTime(DateTime.Now).AddDays(-3));
        _projects.GetAllForDashboardAsync(Arg.Any<CancellationToken>()).Returns([project]);

        var result = await _sut.HandleAsync(new GetDashboardHomeQuery());

        var card = Assert.Single(result.Attention, a => a.Kind == "tasks_overdue");
        Assert.Contains("Late task", card.Sub);
        Assert.Equal($"/projects/{project.Id}/plan/board", card.Link);
    }
}
