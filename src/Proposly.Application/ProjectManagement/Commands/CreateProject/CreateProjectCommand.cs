using Proposly.Application.Abstractions;

namespace Proposly.Application.ProjectManagement.Commands.CreateProject;

public record CreateProjectCommand(
    string Name,
    string? Description,
    string ClientName,
    decimal BudgetAmount,
    string Currency,
    DateOnly StartDate,
    DateOnly? Deadline) : ICommand<Guid>;
