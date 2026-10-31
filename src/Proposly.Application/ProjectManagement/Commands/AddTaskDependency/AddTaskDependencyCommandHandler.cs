using Proposly.Application.Abstractions;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Shared.Exceptions;

namespace Proposly.Application.ProjectManagement.Commands.AddTaskDependency;

public sealed class AddTaskDependencyCommandHandler : ICommandHandler<AddTaskDependencyCommand>
{
    private readonly IProjectRepository _repository;

    public AddTaskDependencyCommandHandler(IProjectRepository repository) => _repository = repository;

    public async Task HandleAsync(AddTaskDependencyCommand command, CancellationToken cancellationToken = default)
    {
        var project = await _repository.GetByIdForWriteAsync(command.ProjectId, cancellationToken)
            ?? throw AppException.NotFound("project.not_found", $"Project {command.ProjectId} not found.");

        var task = project.Tasks.FirstOrDefault(t => t.Id == command.TaskId)
            ?? throw AppException.NotFound("task.not_found", $"Task {command.TaskId} not found.");

        var blocker = project.Tasks.FirstOrDefault(t => t.Id == command.BlockingTaskId)
            ?? throw AppException.NotFound("blocking_task.not_found", $"Blocking task {command.BlockingTaskId} not found.");

        task.AddBlocker(blocker);
        await _repository.UpdateAsync(project, cancellationToken);
    }
}
