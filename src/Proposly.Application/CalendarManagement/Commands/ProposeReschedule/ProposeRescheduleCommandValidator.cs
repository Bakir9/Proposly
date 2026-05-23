using FluentValidation;

namespace Proposly.Application.CalendarManagement.Commands.ProposeReschedule;

public sealed class ProposeRescheduleCommandValidator : AbstractValidator<ProposeRescheduleCommand>
{
    public ProposeRescheduleCommandValidator()
    {
        RuleFor(x => x.ProposedStart).NotEmpty();
        RuleFor(x => x.ProposedEnd).NotEmpty().GreaterThan(x => x.ProposedStart).WithMessage("End must be after start.");
        RuleFor(x => x.Message).MaximumLength(500).When(x => x.Message is not null);
    }
}
