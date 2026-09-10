using FluentValidation;

namespace Proposly.Application.WorkTimeManagement.Commands.ApproveAbsence;

public sealed class ApproveAbsenceCommandValidator : AbstractValidator<ApproveAbsenceCommand>
{
    public ApproveAbsenceCommandValidator()
    {
        RuleFor(x => x.AbsenceId).NotEmpty();
    }
}
