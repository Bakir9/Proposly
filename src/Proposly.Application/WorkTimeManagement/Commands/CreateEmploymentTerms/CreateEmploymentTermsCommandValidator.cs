using FluentValidation;
using Proposly.Domain.WorkTimeManagement.Enums;

namespace Proposly.Application.WorkTimeManagement.Commands.CreateEmploymentTerms;

public sealed class CreateEmploymentTermsCommandValidator
    : AbstractValidator<CreateEmploymentTermsCommand>
{
    public CreateEmploymentTermsCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.ValidFrom)
            .InclusiveBetween(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31));

        RuleFor(x => x.WeeklyHours).GreaterThan(0).LessThanOrEqualTo(60);

        RuleFor(x => x.WorkingDays)
            .NotEqual(WeekDays.None)
            .WithMessage("At least one working weekday is required.");

        RuleFor(x => x.AnnualVacationDays).GreaterThanOrEqualTo(0).LessThanOrEqualTo(366);

        RuleFor(x => x.EmploymentType).IsInEnum();

        RuleFor(x => x.OvertimeLumpSumHours)
            .InclusiveBetween(0, 200)
            .When(x => x.OvertimeLumpSumHours.HasValue);

        // All-in already covers every additional hour; a lump sum on top would compensate the
        // same overtime twice.
        RuleFor(x => x.OvertimeLumpSumHours)
            .Must(v => v is null or 0)
            .When(x => x.IsAllIn)
            .WithMessage(
                "An all-in contract already covers overtime, so it cannot also carry an " +
                "overtime lump sum.");
    }
}
