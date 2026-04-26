using FluentValidation;

namespace Proposly.Application.OfferManagement.Commands.UpdateOffer;

public sealed class UpdateOfferCommandValidator : AbstractValidator<UpdateOfferCommand>
{
    public UpdateOfferCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes is not null);
    }
}
