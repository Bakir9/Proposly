using FluentValidation;

namespace Proposly.Application.WorkTimeManagement.Commands.ApproveTimesheet;

public sealed class ApproveTimesheetCommandValidator : AbstractValidator<ApproveTimesheetCommand>
{
    public ApproveTimesheetCommandValidator()
    {
        RuleFor(x => x.TimesheetId).NotEmpty();
    }
}
