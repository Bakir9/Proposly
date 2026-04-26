using FluentValidation;

namespace Proposly.Application.ProjectManagement.Commands.AddTaskComment;

public sealed class AddTaskCommentCommandValidator : AbstractValidator<AddTaskCommentCommand>
{
    public AddTaskCommentCommandValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MaximumLength(5000);
    }
}
