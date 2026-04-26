using FluentValidation;

namespace Proposly.Application.ProjectManagement.Commands.AddExpense;

public sealed class AddExpenseCommandValidator : AbstractValidator<AddExpenseCommand>
{
    public AddExpenseCommandValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
    }
}
