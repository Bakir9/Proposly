using Proposly.Application.Abstractions;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Shared.Exceptions;

namespace Proposly.Application.ProjectManagement.Commands.UpdateTaskStatus;

public sealed class UpdateTaskStatusCommandHandler : ICommandHandler<UpdateTaskStatusCommand>
{
    private readonly IProjectRepository _repository;

    public UpdateTaskStatusCommandHandler(IProjectRepository repository) => _repository = repository;

    public async Task HandleAsync(UpdateTaskStatusCommand command, CancellationToken cancellationToken = default)
    {
        var project = await _repository.GetByIdForWriteAsync(command.ProjectId, cancellationToken)
            ?? throw AppException.NotFound("project.not_found", $"Project {command.ProjectId} not found.");

        var task = project.Tasks.FirstOrDefault(t => t.Id == command.TaskId)
            ?? throw AppException.NotFound("task.not_found", $"Task {command.TaskId} not found in project {command.ProjectId}.");

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
