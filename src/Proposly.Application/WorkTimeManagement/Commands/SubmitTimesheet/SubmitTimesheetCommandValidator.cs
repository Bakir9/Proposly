using FluentValidation;

namespace Proposly.Application.WorkTimeManagement.Commands.SubmitTimesheet;

public sealed class SubmitTimesheetCommandValidator : AbstractValidator<SubmitTimesheetCommand>
{
    public SubmitTimesheetCommandValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}
