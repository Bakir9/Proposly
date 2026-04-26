using FluentValidation;

namespace Proposly.Application.ProjectManagement.Commands.AddTask;

public sealed class AddTaskCommandValidator : AbstractValidator<AddTaskCommand>
{
    public AddTaskCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        RuleFor(x => x.EstimatedHours).GreaterThan(0).When(x => x.EstimatedHours is not null);
    }
}
