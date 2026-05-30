using Proposly.Application.Abstractions;
using Proposly.Domain.ProjectManagement.Repositories;

namespace Proposly.Application.ProjectManagement.Commands.UpdateTask;

public sealed class UpdateTaskCommandHandler : ICommandHandler<UpdateTaskCommand>
{
    private readonly IProjectRepository _repository;

    public UpdateTaskCommandHandler(IProjectRepository repository) => _repository = repository;

    public async Task HandleAsync(UpdateTaskCommand command, CancellationToken cancellationToken = default)
    {
        var project = await _repository.GetByIdForWriteAsync(command.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        project.UpdateTask(command.TaskId, command.Title, command.Description, command.EstimatedHours, command.ActualHours,
            command.StartDate, command.DueDate, command.MilestoneId, command.AssignedMemberId);

        await _repository.UpdateAsync(project, cancellationToken);
    }
}
