using FluentValidation;

namespace Proposly.Application.ProjectManagement.Commands.UpdateTask;

public sealed class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(50000).When(x => x.Description is not null);
        RuleFor(x => x.EstimatedHours).GreaterThan(0).When(x => x.EstimatedHours is not null);
    }
}
