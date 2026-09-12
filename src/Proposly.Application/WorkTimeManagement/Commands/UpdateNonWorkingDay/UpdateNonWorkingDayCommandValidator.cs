using FluentValidation;
using Proposly.Domain.WorkTimeManagement.Enums;

namespace Proposly.Application.WorkTimeManagement.Commands.UpdateNonWorkingDay;

public sealed class UpdateNonWorkingDayCommandValidator
    : AbstractValidator<UpdateNonWorkingDayCommand>
{
    public UpdateNonWorkingDayCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Kind).IsInEnum();

        RuleFor(x => x.ConsumesVacation)
            .Equal(false)
            .When(x => x.Kind == NonWorkingDayKind.PublicHoliday)
            .WithMessage("A public holiday cannot consume vacation entitlement.");
    }
}
