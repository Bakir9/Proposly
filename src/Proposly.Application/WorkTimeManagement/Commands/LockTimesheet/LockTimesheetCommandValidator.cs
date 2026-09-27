using FluentValidation;

namespace Proposly.Application.WorkTimeManagement.Commands.LockTimesheet;

public sealed class LockTimesheetCommandValidator : AbstractValidator<LockTimesheetCommand>
{
    public LockTimesheetCommandValidator()
    {
        RuleFor(x => x.TimesheetId).NotEmpty();
    }
}
