using FluentValidation;

namespace Proposly.Application.ProjectManagement.Commands.AddProjectNote;

public sealed class AddProjectNoteCommandValidator : AbstractValidator<AddProjectNoteCommand>
{
    public AddProjectNoteCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Content).MaximumLength(200000).When(x => x.Content is not null);
    }
}
