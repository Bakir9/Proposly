using FluentValidation;

namespace Proposly.Application.ProjectManagement.Commands.AddTaskDependency;

public sealed class AddTaskDependencyCommandValidator : AbstractValidator<AddTaskDependencyCommand>
{
    public AddTaskDependencyCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.BlockingTaskId).NotEmpty();
        RuleFor(x => x).Must(x => x.TaskId != x.BlockingTaskId)
            .WithMessage("A task cannot depend on itself.");
    }
}
