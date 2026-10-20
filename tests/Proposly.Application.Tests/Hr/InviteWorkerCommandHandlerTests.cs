using NSubstitute;
using Proposly.Application.Abstractions;
using Proposly.Application.Hr.Commands.InviteWorker;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.CompanyManagement.Enums;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Application.Tests.Hr;

public sealed class InviteWorkerCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICompanyRepository _companies = Substitute.For<ICompanyRepository>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly IAppSettings _appSettings = Substitute.For<IAppSettings>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly InviteWorkerCommandHandler _sut;

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Company _company;

    public InviteWorkerCommandHandlerTests()
    {
        _company = Company.Create(_companyId, "Test GmbH");
        _company.SetPlan(PlanTier.Business, maxUsers: null, maxProjects: null, expiresAt: null);
        _currentUser.CompanyId.Returns(_companyId);
        _companies.GetByIdAsync(_companyId).Returns(_company);
        _appSettings.AppUrl.Returns("https://app.test");
        _sut = new InviteWorkerCommandHandler(_users, _companies, _email, _appSettings, _currentUser);
    }

    [Fact]
    public async Task HandleAsync_ProfileOnlyWorker_GrantsLoginAndSendsInvite()
    {
        var worker = User.CreateProfileOnly(_companyId, "Mujo", "Mujic");
        _users.GetByIdAsync(worker.Id).Returns(worker);
        _users.ExistsByEmailAsync("mujo@test.at").Returns(false);

        await _sut.HandleAsync(new InviteWorkerCommand(worker.Id, "Mujo@Test.at", "Member"));

        Assert.False(worker.IsProfileOnly);
        Assert.True(worker.IsPendingInvite);
        Assert.Equal("mujo@test.at", worker.Email);
        await _email.Received(1).SendAsync(
            "mujo@test.at", Arg.Any<string>(), Arg.Any<string>(), ct: Arg.Any<CancellationToken>());
        await _users.Received(1).UpdateAsync(worker);
    }

    [Fact]
    public async Task HandleAsync_WorkerWithLoginAccess_Throws()
    {
        var user = User.Create(_companyId, "a@b.c", "hash", "A", "B");
        _users.GetByIdAsync(user.Id).Returns(user);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(new InviteWorkerCommand(user.Id, "x@y.z", "Member")));
    }

    [Fact]
    public async Task HandleAsync_UserLimitReached_ThrowsBeforeSendingAnything()
    {
        _company.SetPlan(PlanTier.Free, maxUsers: 1, maxProjects: 3, expiresAt: null);
        var worker = User.CreateProfileOnly(_companyId, "Mujo", "Mujic");
        _users.GetByIdAsync(worker.Id).Returns(worker);
        _users.CountInvitedByCompanyIdAsync(_companyId).Returns(1);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(new InviteWorkerCommand(worker.Id, "x@y.z", "Member")));

        Assert.True(worker.IsProfileOnly);
        await _email.DidNotReceiveWithAnyArgs().SendAsync(default!, default!, default!);
    }

    [Fact]
    public async Task HandleAsync_EmailAlreadyRegistered_Throws()
    {
        var worker = User.CreateProfileOnly(_companyId, "Mujo", "Mujic");
        _users.GetByIdAsync(worker.Id).Returns(worker);
        _users.ExistsByEmailAsync("taken@test.at").Returns(true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(new InviteWorkerCommand(worker.Id, "taken@test.at", "Member")));
    }

    [Fact]
    public async Task HandleAsync_EmailSendFails_NothingIsPersisted()
    {
        var worker = User.CreateProfileOnly(_companyId, "Mujo", "Mujic");
        _users.GetByIdAsync(worker.Id).Returns(worker);
        _email.SendAsync(default!, default!, default!).ReturnsForAnyArgs(
            Task.FromException(new InvalidOperationException("SMTP down")));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(new InviteWorkerCommand(worker.Id, "mujo@test.at", "Member")));

        Assert.True(worker.IsProfileOnly);
        await _users.DidNotReceive().UpdateAsync(Arg.Any<User>());
    }
}
