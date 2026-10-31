using Proposly.Application.Abstractions;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Shared.Exceptions;

namespace Proposly.Application.ProjectManagement.Commands.RemoveTaskDependency;

public sealed class RemoveTaskDependencyCommandHandler : ICommandHandler<RemoveTaskDependencyCommand>
{
    private readonly IProjectRepository _repository;

    public RemoveTaskDependencyCommandHandler(IProjectRepository repository) => _repository = repository;

    public async Task HandleAsync(RemoveTaskDependencyCommand command, CancellationToken cancellationToken = default)
    {
        var project = await _repository.GetByIdForWriteAsync(command.ProjectId, cancellationToken)
            ?? throw AppException.NotFound("project.not_found", $"Project {command.ProjectId} not found.");

        var task = project.Tasks.FirstOrDefault(t => t.Id == command.TaskId)
            ?? throw AppException.NotFound("task.not_found", $"Task {command.TaskId} not found.");

        task.RemoveBlocker(command.BlockingTaskId);
        await _repository.UpdateAsync(project, cancellationToken);
    }
}
