using Proposly.Application.Abstractions;
using Proposly.Domain.ProjectManagement.Repositories;

namespace Proposly.Application.ProjectManagement.Commands.UpdateTaskStatus;

public sealed class UpdateTaskStatusCommandHandler : ICommandHandler<UpdateTaskStatusCommand>
{
    private readonly IProjectRepository _repository;

    public UpdateTaskStatusCommandHandler(IProjectRepository repository) => _repository = repository;

    public async Task HandleAsync(UpdateTaskStatusCommand command, CancellationToken cancellationToken = default)
    {
        var project = await _repository.GetByIdAsync(command.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        var task = project.Tasks.FirstOrDefault(t => t.Id == command.TaskId)
            ?? throw new InvalidOperationException($"Task {command.TaskId} not found in project {command.ProjectId}.");

        switch (command.Status)
        {
            case "Todo":
                task.Reopen();
                break;
            case "InProgress":
                task.Start();
                break;
            case "Done":
                project.CompleteTask(command.TaskId, command.ActualHours);
                break;
            default:
                throw new InvalidOperationException($"Unknown task status '{command.Status}'.");
        }

        await _repository.UpdateAsync(project, cancellationToken);
    }
}
