using NSubstitute;
using Proposly.Application.ProjectManagement.Commands.UpdateTaskStatus;
using Proposly.Domain.ProjectManagement.Entities;
using Proposly.Domain.ProjectManagement.Enums;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Shared.ValueObjects;

namespace Proposly.Application.Tests.ProjectManagement;

public sealed class UpdateTaskStatusCommandHandlerTests
{
    private readonly IProjectRepository _repo = Substitute.For<IProjectRepository>();
    private readonly UpdateTaskStatusCommandHandler _sut;

    public UpdateTaskStatusCommandHandlerTests()
    {
        _sut = new UpdateTaskStatusCommandHandler(_repo);
    }

    private static Project MakeProject()
    {
        return Project.Create(Guid.NewGuid(), "P", null, "Client", Money.Zero("EUR"),
            DateOnly.FromDateTime(DateTime.UtcNow), null);
    }

    [Fact]
    public async Task HandleAsync_InProgress_TransitionsTask()
    {
        var project = MakeProject();
        var task = project.AddTask("T", null, null, null, null);
        _repo.GetByIdAsync(project.Id).Returns(project);

        await _sut.HandleAsync(new UpdateTaskStatusCommand(project.Id, task.Id, "InProgress", null));

        Assert.Equal(ProjectTaskStatus.InProgress, task.Status);
        await _repo.Received(1).UpdateAsync(project, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Done_SetsActualHours()
    {
        var project = MakeProject();
        var task = project.AddTask("T", null, null, null, null);
        task.Start();
        _repo.GetByIdAsync(project.Id).Returns(project);

        await _sut.HandleAsync(new UpdateTaskStatusCommand(project.Id, task.Id, "Done", 5m));

        Assert.Equal(ProjectTaskStatus.Done, task.Status);
        Assert.Equal(5m, task.ActualHours);
    }

    [Fact]
    public async Task HandleAsync_Todo_ReopensTask()
    {
        var project = MakeProject();
        var task = project.AddTask("T", null, null, null, null);
        task.Start();
        _repo.GetByIdAsync(project.Id).Returns(project);

        await _sut.HandleAsync(new UpdateTaskStatusCommand(project.Id, task.Id, "Todo", null));

        Assert.Equal(ProjectTaskStatus.Todo, task.Status);
    }

    [Fact]
    public async Task HandleAsync_ProjectNotFound_Throws()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>()).Returns((Project?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(new UpdateTaskStatusCommand(Guid.NewGuid(), Guid.NewGuid(), "InProgress", null)));
    }

    [Fact]
    public async Task HandleAsync_TaskNotFound_Throws()
    {
        var project = MakeProject();
        _repo.GetByIdAsync(project.Id).Returns(project);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(new UpdateTaskStatusCommand(project.Id, Guid.NewGuid(), "InProgress", null)));
    }

    [Fact]
    public async Task HandleAsync_UnknownStatus_Throws()
    {
        var project = MakeProject();
        var task = project.AddTask("T", null, null, null, null);
        _repo.GetByIdAsync(project.Id).Returns(project);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.HandleAsync(new UpdateTaskStatusCommand(project.Id, task.Id, "Bogus", null)));
    }
}
