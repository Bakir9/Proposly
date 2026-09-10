using FluentValidation;

namespace Proposly.Application.WorkTimeManagement.Commands.RequestAbsence;

public sealed class RequestAbsenceCommandValidator : AbstractValidator<RequestAbsenceCommand>
{
    public RequestAbsenceCommandValidator()
    {
        RuleFor(x => x.Type).IsInEnum();

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("The end date cannot be before the start date.");

        RuleFor(x => x.StartDate)
            .InclusiveBetween(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31));

        // Guards against a typo turning into a multi-year absence.
        RuleFor(x => x)
            .Must(x => x.EndDate.DayNumber - x.StartDate.DayNumber <= 366)
            .WithMessage("An absence cannot span more than a year.")
            .WithName(nameof(RequestAbsenceCommand.EndDate));

        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
