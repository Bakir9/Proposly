using FluentValidation;
using Proposly.Domain.WorkTimeManagement.Enums;

namespace Proposly.Application.WorkTimeManagement.Commands.CreateNonWorkingDay;

public sealed class CreateNonWorkingDayCommandValidator
    : AbstractValidator<CreateNonWorkingDayCommand>
{
    public CreateNonWorkingDayCommandValidator()
    {
        RuleFor(x => x.Date)
            .InclusiveBetween(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31));

        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Kind).IsInEnum();

        // A statutory holiday costing the employee a vacation day would be wrong in every
        // jurisdiction this module targets.
        RuleFor(x => x.ConsumesVacation)
            .Must(v => v != true)
            .When(x => x.Kind == NonWorkingDayKind.PublicHoliday)
            .WithMessage("A public holiday cannot consume vacation entitlement.");
    }
}
