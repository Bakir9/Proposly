using FluentValidation;

namespace Proposly.Application.CalendarManagement.Commands.RescheduleTermin;

public sealed class RescheduleTerminCommandValidator : AbstractValidator<RescheduleTerminCommand>
{
    public RescheduleTerminCommandValidator()
    {
        RuleFor(x => x.NewStart).NotEmpty();
        RuleFor(x => x.NewEnd).NotEmpty().GreaterThan(x => x.NewStart).WithMessage("End must be after start.");
    }
}
