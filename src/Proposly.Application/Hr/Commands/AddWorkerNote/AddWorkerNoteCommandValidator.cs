using FluentValidation;
using Proposly.Domain.HrManagement.Entities;

namespace Proposly.Application.Hr.Commands.AddWorkerNote;

public sealed class AddWorkerNoteCommandValidator : AbstractValidator<AddWorkerNoteCommand>
{
    public AddWorkerNoteCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Tag).Must(t => EmployeeNote.Tags.Contains(t)).WithMessage("Unknown note tag.");
        RuleFor(x => x.Text).NotEmpty().MaximumLength(4000);
    }
}
