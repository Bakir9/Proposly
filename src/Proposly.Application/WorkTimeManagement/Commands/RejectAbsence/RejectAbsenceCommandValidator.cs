using FluentValidation;

namespace Proposly.Application.WorkTimeManagement.Commands.RejectAbsence;

public sealed class RejectAbsenceCommandValidator : AbstractValidator<RejectAbsenceCommand>
{
    public RejectAbsenceCommandValidator()
    {
        RuleFor(x => x.AbsenceId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
