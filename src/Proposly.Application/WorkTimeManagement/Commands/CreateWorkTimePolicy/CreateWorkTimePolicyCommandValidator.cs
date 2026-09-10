using FluentValidation;

namespace Proposly.Application.WorkTimeManagement.Commands.CreateWorkTimePolicy;

public sealed class CreateWorkTimePolicyCommandValidator
    : AbstractValidator<CreateWorkTimePolicyCommand>
{
    public CreateWorkTimePolicyCommandValidator()
    {
        RuleFor(x => x.Jurisdiction).NotEmpty().MaximumLength(10);
        RuleFor(x => x.HolidayRegionCode).MaximumLength(20);

        RuleFor(x => x.MaxHoursPerDay).GreaterThan(0).LessThanOrEqualTo(24);
        RuleFor(x => x.MaxHoursPerWeek).GreaterThan(0).LessThanOrEqualTo(168);
        RuleFor(x => x.AveragingWindowWeeks).InclusiveBetween(1, 104);
        RuleFor(x => x.MaxAverageHoursPerWeek).GreaterThan(0).LessThanOrEqualTo(168);
        RuleFor(x => x.MinDailyRestHours).GreaterThanOrEqualTo(0).LessThanOrEqualTo(24);
        RuleFor(x => x.MinWeeklyRestHours).GreaterThanOrEqualTo(0).LessThanOrEqualTo(168);

        // A cap is a ceiling on surplus and a floor is a limit on deficit, so their signs are
        // fixed. Null in either means unbounded.
        RuleFor(x => x.SurplusCapHours).GreaterThanOrEqualTo(0).When(x => x.SurplusCapHours.HasValue);
        RuleFor(x => x.DeficitFloorHours).LessThanOrEqualTo(0).When(x => x.DeficitFloorHours.HasValue);

        RuleFor(x => x.MaxHoursPerWeek)
            .GreaterThanOrEqualTo(x => x.MaxHoursPerDay)
            .WithMessage("The weekly maximum cannot be lower than the daily maximum.");

        RuleForEach(x => x.BreakRules).ChildRules(rule =>
        {
            rule.RuleFor(r => r.AboveHours).GreaterThanOrEqualTo(0).LessThanOrEqualTo(24);
            rule.RuleFor(r => r.MinBreakMinutes).GreaterThan(0).LessThanOrEqualTo(8 * 60);
        });

        RuleFor(x => x.BreakRules)
            .Must(rules => rules.Select(r => r.AboveHours).Distinct().Count() == rules.Count)
            .WithMessage("Break tiers must have distinct thresholds.");
    }
}
