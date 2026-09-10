using FluentValidation;

namespace Proposly.Application.WorkTimeManagement.Commands.CancelAbsence;

public sealed class CancelAbsenceCommandValidator : AbstractValidator<CancelAbsenceCommand>
{
    public CancelAbsenceCommandValidator()
    {
        RuleFor(x => x.AbsenceId).NotEmpty();
    }
}
