using Proposly.Application.Abstractions;

namespace Proposly.Application.ProjectManagement.Commands.CreateProject;

public record CreateProjectCommand(
    string Name,
    string? Description,
    Guid ClientId,
    decimal BudgetAmount,
    string Currency,
    DateOnly StartDate,
    DateOnly? Deadline) : ICommand<Guid>;
