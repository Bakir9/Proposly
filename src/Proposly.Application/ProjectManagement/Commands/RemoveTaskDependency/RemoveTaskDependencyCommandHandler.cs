using Proposly.Application.Abstractions;
using Proposly.Domain.ProjectManagement.Repositories;

namespace Proposly.Application.ProjectManagement.Commands.RemoveTaskDependency;

public sealed class RemoveTaskDependencyCommandHandler : ICommandHandler<RemoveTaskDependencyCommand>
{
    private readonly IProjectRepository _repository;

    public RemoveTaskDependencyCommandHandler(IProjectRepository repository) => _repository = repository;

    public async Task HandleAsync(RemoveTaskDependencyCommand command, CancellationToken cancellationToken = default)
    {
        var project = await _repository.GetByIdForWriteAsync(command.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        var task = project.Tasks.FirstOrDefault(t => t.Id == command.TaskId)
            ?? throw new InvalidOperationException($"Task {command.TaskId} not found.");

        task.RemoveBlocker(command.BlockingTaskId);
        await _repository.UpdateAsync(project, cancellationToken);
    }
}
