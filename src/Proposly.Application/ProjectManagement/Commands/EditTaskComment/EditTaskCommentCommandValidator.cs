using FluentValidation;

namespace Proposly.Application.ProjectManagement.Commands.EditTaskComment;

public sealed class EditTaskCommentCommandValidator : AbstractValidator<EditTaskCommentCommand>
{
    public EditTaskCommentCommandValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MaximumLength(5000);
    }
}
