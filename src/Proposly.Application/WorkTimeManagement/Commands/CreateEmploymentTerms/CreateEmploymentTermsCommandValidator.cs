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
    }
}
