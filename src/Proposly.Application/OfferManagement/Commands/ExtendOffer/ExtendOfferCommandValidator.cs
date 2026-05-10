using FluentValidation;

namespace Proposly.Application.OfferManagement.Commands.ExtendOffer;

public sealed class ExtendOfferCommandValidator : AbstractValidator<ExtendOfferCommand>
{
    public ExtendOfferCommandValidator()
    {
        RuleFor(x => x.OfferId).NotEmpty();
        RuleFor(x => x.NewValidUntil)
            .Must(d => d > DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("New validity date must be in the future.");
    }
}
