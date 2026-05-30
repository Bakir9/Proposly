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

        RuleFor(x => x.ActualHours)
            .GreaterThan(0).WithMessage("Actual hours must be greater than zero.")
            .When(x => x.ActualHours is not null);
    }
}
