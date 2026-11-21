using FluentValidation;

namespace Proposly.Application.ProjectManagement.Commands.UpdateTaskStatus;

public sealed class UpdateTaskStatusCommandValidator : AbstractValidator<UpdateTaskStatusCommand>
{
    public UpdateTaskStatusCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.Status).NotEmpty()
            .Must(s => s is "Todo" or "InProgress" or "Done")
            .WithMessage("Invalid task status.");

        // 0 is a legitimate value: checking off a template step that took no separate time.
        RuleFor(x => x.ActualHours)
            .GreaterThanOrEqualTo(0).WithMessage("Actual hours cannot be negative.")
            .When(x => x.ActualHours is not null);
    }
}
