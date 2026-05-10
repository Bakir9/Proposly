using FluentValidation;

namespace Proposly.Application.OfferManagement.Commands.AddClientNote;

public sealed class AddClientNoteCommandValidator : AbstractValidator<AddClientNoteCommand>
{
    public AddClientNoteCommandValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
    }
}
