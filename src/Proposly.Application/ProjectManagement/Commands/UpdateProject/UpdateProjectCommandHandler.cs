using Proposly.Application.Abstractions;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Shared.ValueObjects;

namespace Proposly.Application.ProjectManagement.Commands.UpdateProject;

public sealed class UpdateProjectCommandHandler : ICommandHandler<UpdateProjectCommand>
{
    private readonly IProjectRepository _repository;

    public UpdateProjectCommandHandler(IProjectRepository repository) => _repository = repository;

    public async Task HandleAsync(UpdateProjectCommand command, CancellationToken cancellationToken = default)
    {
        var project = await _repository.GetByIdAsync(command.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        if (command.Status is not null && command.Status != project.Status.ToString())
        {
            switch (command.Status)
            {
                case "Active":
                    project.Activate();
                    break;
                case "OnHold":
                    project.PutOnHold();
                    break;
                case "Completed":
                    project.Complete();
                    break;
                case "Cancelled":
                    project.Cancel();
                    break;
            }
        }

        var budget = command.BudgetAmount is not null && command.BudgetCurrency is not null
            ? new Money(command.BudgetAmount.Value, command.BudgetCurrency)
            : null;
        project.UpdateDetails(command.Name, command.Description, command.Deadline, budget);
        await _repository.UpdateAsync(project, cancellationToken);
    }
}
