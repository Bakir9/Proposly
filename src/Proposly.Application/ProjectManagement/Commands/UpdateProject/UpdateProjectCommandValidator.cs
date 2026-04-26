using FluentValidation;

namespace Proposly.Application.ProjectManagement.Commands.UpdateProject;

public sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        RuleFor(x => x.BudgetAmount).GreaterThanOrEqualTo(0).When(x => x.BudgetAmount is not null);
        RuleFor(x => x.BudgetCurrency).Length(3).When(x => x.BudgetCurrency is not null);
    }
}
