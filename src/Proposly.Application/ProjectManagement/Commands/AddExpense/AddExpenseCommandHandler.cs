using Proposly.Application.Abstractions;
using Proposly.Domain.ProjectManagement.Enums;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Shared.ValueObjects;

namespace Proposly.Application.ProjectManagement.Commands.AddExpense;

public sealed class AddExpenseCommandHandler : ICommandHandler<AddExpenseCommand, Guid>
{
    private readonly IProjectRepository _repository;

    public AddExpenseCommandHandler(IProjectRepository repository) => _repository = repository;

    public async Task<Guid> HandleAsync(AddExpenseCommand command, CancellationToken cancellationToken = default)
    {
        var project = await _repository.GetByIdForWriteAsync(command.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        var category = Enum.Parse<ExpenseCategory>(command.Category, ignoreCase: true);
        var expense = project.AddExpense(command.Description, new Money(command.Amount, command.Currency), category, command.Date);
        await _repository.UpdateAsync(project, cancellationToken);
        return expense.Id;
    }
}
