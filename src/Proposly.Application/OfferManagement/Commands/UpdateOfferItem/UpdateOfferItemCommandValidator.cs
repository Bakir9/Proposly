using FluentValidation;

namespace Proposly.Application.OfferManagement.Commands.UpdateOfferItem;

public sealed class UpdateOfferItemCommandValidator : AbstractValidator<UpdateOfferItemCommand>
{
    public UpdateOfferItemCommandValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
    }
}
