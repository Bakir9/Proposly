using FluentValidation;

namespace Proposly.Application.WorkTimeManagement.Commands.SetEntitlement;

public sealed class SetEntitlementCommandValidator : AbstractValidator<SetEntitlementCommand>
{
    public SetEntitlementCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.EntitledDays).GreaterThanOrEqualTo(0).LessThanOrEqualTo(366);
        RuleFor(x => x.CarriedOverDays).GreaterThanOrEqualTo(0).LessThanOrEqualTo(366);
    }
}
