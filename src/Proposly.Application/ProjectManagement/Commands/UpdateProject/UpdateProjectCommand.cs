using Proposly.Application.Abstractions;

namespace Proposly.Application.ProjectManagement.Commands.UpdateProject;

public record UpdateProjectCommand(Guid ProjectId, string Name, string? Description, DateOnly? Deadline, string? Status, decimal? BudgetAmount, string? BudgetCurrency, Guid? ClientId = null) : ICommand;
