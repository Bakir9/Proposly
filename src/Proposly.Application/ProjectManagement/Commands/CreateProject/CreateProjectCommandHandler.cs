using Proposly.Application.Abstractions;
using Proposly.Domain.ProjectManagement.Entities;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Shared.ValueObjects;

namespace Proposly.Application.ProjectManagement.Commands.CreateProject;

public sealed class CreateProjectCommandHandler : ICommandHandler<CreateProjectCommand, Guid>
{
    private readonly IProjectRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public CreateProjectCommandHandler(IProjectRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Guid> HandleAsync(CreateProjectCommand command, CancellationToken ct = default)
    {
        var project = Project.Create(
            _currentUser.CompanyId,
            command.Name,
            command.Description,
            command.ClientName,
            new Money(command.BudgetAmount, command.Currency),
            command.StartDate,
            command.Deadline);

        await _repository.AddAsync(project, ct);
        return project.Id;
    }
}
