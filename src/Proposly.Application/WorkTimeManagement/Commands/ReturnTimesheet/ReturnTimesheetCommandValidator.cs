using FluentValidation;

namespace Proposly.Application.WorkTimeManagement.Commands.ReturnTimesheet;

public sealed class ReturnTimesheetCommandValidator : AbstractValidator<ReturnTimesheetCommand>
{
    public ReturnTimesheetCommandValidator()
    {
        RuleFor(x => x.TimesheetId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
