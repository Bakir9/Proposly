using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.OfferManagement.Repositories;
using Proposly.Domain.ProjectManagement.Entities;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Shared.ValueObjects;

namespace Proposly.Application.ProjectManagement.Commands.CreateProject;

public sealed class CreateProjectCommandHandler : ICommandHandler<CreateProjectCommand, Guid>
{
    private readonly IProjectRepository _repository;
    private readonly IClientRepository _clients;
    private readonly ICompanyRepository _companies;
    private readonly ICurrentUserService _currentUser;

    public CreateProjectCommandHandler(IProjectRepository repository, IClientRepository clients, ICompanyRepository companies, ICurrentUserService currentUser)
    {
        _repository = repository;
        _clients = clients;
        _companies = companies;
        _currentUser = currentUser;
    }

    public async Task<Guid> HandleAsync(CreateProjectCommand command, CancellationToken ct = default)
    {
        var company = await _companies.GetByIdAsync(_currentUser.CompanyId, ct)
            ?? throw new InvalidOperationException("Company not found.");

        var projectCount = await _repository.CountByCompanyIdAsync(_currentUser.CompanyId, ct);
        if (company.IsProjectLimitReached(projectCount))
            throw new InvalidOperationException($"Project limit reached for your current plan. Upgrade to create more projects.");

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
