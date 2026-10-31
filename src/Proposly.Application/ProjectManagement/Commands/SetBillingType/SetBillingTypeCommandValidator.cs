using FluentValidation;
using Proposly.Domain.ProjectManagement.Enums;

namespace Proposly.Application.ProjectManagement.Commands.SetBillingType;

public sealed class SetBillingTypeCommandValidator : AbstractValidator<SetBillingTypeCommand>
{
    public SetBillingTypeCommandValidator()
    {
        RuleFor(c => c.ProjectId).NotEmpty();
        RuleFor(c => c.BillingType)
            .Must(v => Enum.TryParse<BillingType>(v, out _))
            .WithMessage("Billing type must be FixedPrice, TimeAndMaterials or Internal.");
    }
}
