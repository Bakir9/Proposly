using Proposly.Application.Abstractions;
using Proposly.Domain.ProjectManagement.Repositories;

namespace Proposly.Application.ProjectManagement.Commands.AddTask;

public sealed class AddTaskCommandHandler : ICommandHandler<AddTaskCommand, Guid>
{
    private readonly IProjectRepository _repository;

    public AddTaskCommandHandler(IProjectRepository repository) => _repository = repository;

    public async Task<Guid> HandleAsync(AddTaskCommand command, CancellationToken cancellationToken = default)
    {
        var project = await _repository.GetByIdAsync(command.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        var task = project.AddTask(command.Title, command.Description, command.EstimatedHours, command.DueDate, command.MilestoneId, command.AssignedMemberId);
        await _repository.UpdateAsync(project, cancellationToken);
        return task.Id;
    }
}
