using FluentValidation;
using Proposly.Domain.HrManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;

namespace Proposly.Application.Hr.Commands.UpdateWorker;

public sealed class UpdateWorkerCommandValidator : AbstractValidator<UpdateWorkerCommand>
{
    public UpdateWorkerCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.JobTitle).MaximumLength(150);
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.Gender).MaximumLength(40);
        RuleFor(x => x.Svnr).MaximumLength(40);
        RuleFor(x => x.Nationality).MaximumLength(100);
        RuleFor(x => x.Department).MaximumLength(100);
        RuleFor(x => x.Location).MaximumLength(150);
        RuleFor(x => x.Address).MaximumLength(300);
        RuleFor(x => x.EmergencyName).MaximumLength(150);
        RuleFor(x => x.EmergencyRelation).MaximumLength(100);
        RuleFor(x => x.EmergencyPhone).MaximumLength(50);
        RuleFor(x => x.CvJson).MaximumLength(100_000);

        RuleFor(x => x.DayHours)
            .Must(d => d == null || d.Count == 7)
            .WithMessage("DayHours must have exactly 7 entries (Monday first).");
        RuleForEach(x => x.DayHours)
            .Must(h => h is null or (>= 0 and <= 24))
            .WithMessage("Daily hours must be between 0 and 24.");

        RuleFor(x => x)
            .Must(x => x.WorkModel == WorkModel.Hourly
                || (x.DayHours != null && x.DayHours.Any(h => h is > 0)))
            .WithName(nameof(UpdateWorkerCommand.DayHours))
            .WithMessage("A scheduled work model needs at least one day with hours.");

        RuleFor(x => x.HourlyRateAmount).GreaterThanOrEqualTo(0).When(x => x.HourlyRateAmount.HasValue);
        RuleFor(x => x.HourlyRateCurrency).Length(3).When(x => x.HourlyRateCurrency != null);
        RuleFor(x => x.MaxHoursPerMonth).InclusiveBetween(0, 744).When(x => x.MaxHoursPerMonth.HasValue);
        RuleFor(x => x.ExpectedWeeklyHours).InclusiveBetween(0, 60).When(x => x.ExpectedWeeklyHours.HasValue);
        RuleFor(x => x.OvertimeSurchargePct).InclusiveBetween(0, 400).When(x => x.OvertimeSurchargePct.HasValue);
        RuleFor(x => x.NightSurchargePct).InclusiveBetween(0, 400).When(x => x.NightSurchargePct.HasValue);
        RuleFor(x => x.SundaySurchargePct).InclusiveBetween(0, 400).When(x => x.SundaySurchargePct.HasValue);
        RuleFor(x => x.HolidaySurchargePct).InclusiveBetween(0, 400).When(x => x.HolidaySurchargePct.HasValue);
        RuleFor(x => x.HomeOfficeDaysPerWeek).InclusiveBetween(0, 7).When(x => x.HomeOfficeDaysPerWeek.HasValue);
        RuleFor(x => x.MaxHoursPerDay).InclusiveBetween(0, 24).When(x => x.MaxHoursPerDay.HasValue);
        RuleFor(x => x.AnnualVacationDays).InclusiveBetween(0, 366);
        RuleFor(x => x.CarriedOverDays).InclusiveBetween(0, 366);

        RuleForEach(x => x.Allowances).ChildRules(a =>
        {
            a.RuleFor(y => y.Name).NotEmpty().MaximumLength(150);
            a.RuleFor(y => y.Amount).GreaterThanOrEqualTo(0);
            a.RuleFor(y => y.Currency).NotEmpty().Length(3);
            a.RuleFor(y => y.Unit).Must(u => EmployeeAllowance.Units.Contains(u))
                .WithMessage("Unknown allowance unit.");
        });
    }
}
