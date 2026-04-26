using FluentValidation;

namespace Proposly.Application.OfferManagement.Commands.CreateClient;

public sealed class CreateClientCommandValidator : AbstractValidator<CreateClientCommand>
{
    public CreateClientCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactPerson).MaximumLength(200).When(x => x.ContactPerson is not null);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(200).When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Phone).MaximumLength(50).When(x => x.Phone is not null);
        RuleFor(x => x.PostalCode).MaximumLength(20).When(x => x.PostalCode is not null);
    }
}
