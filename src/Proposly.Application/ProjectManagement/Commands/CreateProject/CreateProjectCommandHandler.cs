using Proposly.Application.Abstractions;
using Proposly.Domain.OfferManagement.Repositories;
using Proposly.Domain.ProjectManagement.Entities;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Shared.ValueObjects;

namespace Proposly.Application.ProjectManagement.Commands.CreateProject;

public sealed class CreateProjectCommandHandler : ICommandHandler<CreateProjectCommand, Guid>
{
    private readonly IProjectRepository _repository;
    private readonly IClientRepository _clients;
    private readonly ICurrentUserService _currentUser;

    public CreateProjectCommandHandler(IProjectRepository repository, IClientRepository clients, ICurrentUserService currentUser)
    {
        _repository = repository;
        _clients = clients;
        _currentUser = currentUser;
    }

    public async Task<Guid> HandleAsync(CreateProjectCommand command, CancellationToken ct = default)
    {
        var client = await _clients.GetByIdAsync(command.ClientId, ct)
            ?? throw new InvalidOperationException($"Client {command.ClientId} not found.");

        if (client.CompanyId != _currentUser.CompanyId)
            throw new UnauthorizedAccessException("Client does not belong to your company.");

        var project = Project.Create(
            _currentUser.CompanyId,
            command.Name,
            command.Description,
            client.Id,
            client.Name,
            new Money(command.BudgetAmount, command.Currency),
            command.StartDate,
            command.Deadline);

        await _repository.AddAsync(project, ct);
        return project.Id;
    }
}
