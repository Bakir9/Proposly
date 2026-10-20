using FluentValidation;

namespace Proposly.Application.Hr.Commands.AddWorkerAbsence;

public sealed class AddWorkerAbsenceCommandValidator : AbstractValidator<AddWorkerAbsenceCommand>
{
    public AddWorkerAbsenceCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date cannot precede start date.");
    }
}
