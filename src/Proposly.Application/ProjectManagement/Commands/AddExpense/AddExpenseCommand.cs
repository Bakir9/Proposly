using Proposly.Application.Abstractions;

namespace Proposly.Application.ProjectManagement.Commands.AddExpense;

public record AddExpenseCommand(Guid ProjectId, string Description, decimal Amount, string Currency, string Category, DateOnly Date) : ICommand<Guid>;
