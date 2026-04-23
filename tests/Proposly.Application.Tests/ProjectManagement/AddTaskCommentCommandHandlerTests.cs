using NSubstitute;
using Proposly.Application.Abstractions;
using Proposly.Application.ProjectManagement.Commands.AddTaskComment;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.ProjectManagement.Entities;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Shared.ValueObjects;

namespace Proposly.Application.Tests.ProjectManagement;

public sealed class AddTaskCommentCommandHandlerTests
{
    private readonly IProjectRepository _projects = Substitute.For<IProjectRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly AddTaskCommentCommandHandler _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _companyId = Guid.NewGuid();

    public AddTaskCommentCommandHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _sut = new AddTaskCommentCommandHandler(_projects, _users, _currentUser);
    }

    private Project MakeProject()
    {
        return Project.Create(_companyId, "P", null, "Client", Money.Zero("EUR"),
            DateOnly.FromDateTime(DateTime.UtcNow), null);
    }

    private User MakeUser() =>
        User.Create(_companyId, "bob@test.com", "hash", "Bob", "Jones");

    [Fact]
    public async Task HandleAsync_ValidInput_AddsCommentAndReturnsId()
    {
        var project = MakeProject();
        var task = project.AddTask("T", null, null, null, null);
        var user = MakeUser();

        _projects.GetByIdAsync(project.Id).Returns(project);
        _users.GetByIdAsync(_userId).Returns(user);

        var cmd = new AddTaskCommentCommand(project.Id, task.Id, "Great work!");
        var commentId = await _sut.HandleAsync(cmd);

        Assert.NotEqual(Guid.Empty, commentId);
        Assert.Single(task.Comments);
        Assert.Equal("Great work!", task.Comments.Single().Body);
        Assert.Equal("Bob Jones", task.Comments.Single().AuthorName);
    }

    [Fact]
    public async Task HandleAsync_ValidInput_PersistsProject()
    {
        var project = MakeProject();
        var task = project.AddTask("T", null, null, null, null);
        var user = MakeUser();

        _projects.GetByIdAsync(project.Id).Returns(project);
        _users.GetByIdAsync(_userId).Returns(user);

        await _sut.HandleAsync(new AddTaskCommentCommand(project.Id, task.Id, "LGTM"));

        await _projects.Received(1).UpdateAsync(project, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ProjectNotFound_Throws()
    {
        _projects.GetByIdAsync(Arg.Any<Guid>()).Returns((Project?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(new AddTaskCommentCommand(Guid.NewGuid(), Guid.NewGuid(), "body")));
    }

    [Fact]
    public async Task HandleAsync_UserNotFound_Throws()
    {
        var project = MakeProject();
        var task = project.AddTask("T", null, null, null, null);

        _projects.GetByIdAsync(project.Id).Returns(project);
        _users.GetByIdAsync(_userId).Returns((User?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(new AddTaskCommentCommand(project.Id, task.Id, "body")));
    }

    [Fact]
    public async Task HandleAsync_SnapshotsAuthorName()
    {
        var project = MakeProject();
        var task = project.AddTask("T", null, null, null, null);
        var user = User.Create(_companyId, "x@x.com", "hash", "Jane", "Doe");

        _projects.GetByIdAsync(project.Id).Returns(project);
        _users.GetByIdAsync(_userId).Returns(user);

        await _sut.HandleAsync(new AddTaskCommentCommand(project.Id, task.Id, "hello"));

        Assert.Equal("Jane Doe", task.Comments.Single().AuthorName);
        Assert.Equal(_userId, task.Comments.Single().AuthorId);
    }
}
