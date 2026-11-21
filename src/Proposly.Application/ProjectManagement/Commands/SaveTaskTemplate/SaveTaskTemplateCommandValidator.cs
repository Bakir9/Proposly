using FluentValidation;

namespace Proposly.Application.ProjectManagement.Commands.SaveTaskTemplate;

public sealed class SaveTaskTemplateCommandValidator : AbstractValidator<SaveTaskTemplateCommand>
{
    public SaveTaskTemplateCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Steps)
            .NotEmpty().WithMessage("A template needs at least one step.")
            .Must(s => s.Count <= 50).WithMessage("A template can hold at most 50 steps.");
        RuleForEach(x => x.Steps).ChildRules(step =>
        {
            step.RuleFor(s => s.Title).NotEmpty().MaximumLength(200);
            step.RuleFor(s => s.EstimatedHours).GreaterThan(0).When(s => s.EstimatedHours is not null);
        });
    }
}
