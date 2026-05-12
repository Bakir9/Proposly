using Proposly.Application.Abstractions;
using Proposly.Domain.OfferManagement.Repositories;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Shared.ValueObjects;

namespace Proposly.Application.ProjectManagement.Commands.UpdateProject;

public sealed class UpdateProjectCommandHandler : ICommandHandler<UpdateProjectCommand>
{
    private readonly IProjectRepository _repository;
    private readonly IClientRepository _clients;
    private readonly ICurrentUserService _currentUser;

    public UpdateProjectCommandHandler(IProjectRepository repository, IClientRepository clients, ICurrentUserService currentUser)
    {
        _repository = repository;
        _clients = clients;
        _currentUser = currentUser;
    }

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

        if (command.ClientId.HasValue)
        {
            var client = await _clients.GetByIdAsync(command.ClientId.Value, cancellationToken)
                ?? throw new InvalidOperationException($"Client {command.ClientId} not found.");

            if (client.CompanyId != _currentUser.CompanyId)
                throw new UnauthorizedAccessException("Client does not belong to your company.");

            project.UpdateClient(client.Id, client.Name);
        }

        var budget = command.BudgetAmount is not null && command.BudgetCurrency is not null
            ? new Money(command.BudgetAmount.Value, command.BudgetCurrency)
            : null;
        project.UpdateDetails(command.Name, command.Description, command.Deadline, budget);
        await _repository.UpdateAsync(project, cancellationToken);
    }
}
