using FluentValidation;

namespace Proposly.Application.ProjectManagement.Commands.UpdateProjectNote;

public sealed class UpdateProjectNoteCommandValidator : AbstractValidator<UpdateProjectNoteCommand>
{
    public UpdateProjectNoteCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.NoteId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Content).MaximumLength(200000).When(x => x.Content is not null);
    }
}
