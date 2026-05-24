using FluentValidation;

namespace Proposly.Application.OfferManagement.Commands.SendOfferEmail;

public sealed class SendOfferEmailCommandValidator : AbstractValidator<SendOfferEmailCommand>
{
    public SendOfferEmailCommandValidator()
    {
        RuleFor(x => x.OfferId).NotEmpty();

        When(x => x.CustomSubject is not null, () =>
            RuleFor(x => x.CustomSubject).NotEmpty().MaximumLength(200));

        When(x => x.CustomBody is not null, () =>
            RuleFor(x => x.CustomBody).NotEmpty());
    }
}
