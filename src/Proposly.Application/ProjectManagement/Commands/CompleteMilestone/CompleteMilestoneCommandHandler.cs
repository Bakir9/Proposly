using Proposly.Application.Abstractions;
using Proposly.Domain.ProjectManagement.Repositories;

namespace Proposly.Application.ProjectManagement.Commands.CompleteMilestone;

public sealed class CompleteMilestoneCommandHandler : ICommandHandler<CompleteMilestoneCommand>
{
    private readonly IProjectRepository _repository;

    public CompleteMilestoneCommandHandler(IProjectRepository repository) => _repository = repository;

    public async Task HandleAsync(CompleteMilestoneCommand command, CancellationToken cancellationToken = default)
    {
        var project = await _repository.GetByIdAsync(command.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        var milestone = project.Milestones.FirstOrDefault(m => m.Id == command.MilestoneId)
            ?? throw new InvalidOperationException($"Milestone {command.MilestoneId} not found in project {command.ProjectId}.");

        milestone.Complete();
        await _repository.UpdateAsync(project, cancellationToken);
    }
}
