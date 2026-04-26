using FluentValidation;

namespace Proposly.Application.OfferManagement.Commands.AddOfferItem;

public sealed class AddOfferItemCommandValidator : AbstractValidator<AddOfferItemCommand>
{
    public AddOfferItemCommandValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
    }
}
