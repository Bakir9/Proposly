using Proposly.Application.Abstractions;
using Proposly.Domain.ProjectManagement.Repositories;

namespace Proposly.Application.ProjectManagement.Commands.AddMilestone;

public sealed class AddMilestoneCommandHandler : ICommandHandler<AddMilestoneCommand, Guid>
{
    private readonly IProjectRepository _repository;

    public AddMilestoneCommandHandler(IProjectRepository repository) => _repository = repository;

    public async Task<Guid> HandleAsync(AddMilestoneCommand command, CancellationToken cancellationToken = default)
    {
        var project = await _repository.GetByIdForWriteAsync(command.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        var milestone = project.AddMilestone(command.Title, command.DueDate);
        await _repository.UpdateAsync(project, cancellationToken);
        return milestone.Id;
    }
}
