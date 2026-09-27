using FluentValidation;

namespace Proposly.Application.WorkTimeManagement.Commands.ReopenTimesheet;

public sealed class ReopenTimesheetCommandValidator : AbstractValidator<ReopenTimesheetCommand>
{
    public ReopenTimesheetCommandValidator()
    {
        RuleFor(x => x.TimesheetId).NotEmpty();
    }
}
