using FluentValidation;

namespace Proposly.Application.WorkTimeManagement.Commands.UpsertWorkDay;

public sealed class UpsertWorkDayCommandValidator : AbstractValidator<UpsertWorkDayCommand>
{
    public UpsertWorkDayCommandValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
        RuleFor(x => x.BreakMinutes).GreaterThanOrEqualTo(0).LessThan(24 * 60);
        RuleFor(x => x.Note).MaximumLength(500);

        RuleFor(x => x.Date)
            .Must((cmd, date) => date.Year == cmd.Year && date.Month == cmd.Month)
            .WithMessage("Date must fall within the timesheet's year and month.");

        // A same-day shift must end after it starts. Overnight shifts are legitimate but must be
        // marked, because start and end times alone cannot tell an intended night shift from a
        // transposed pair of times.
        RuleFor(x => x.EndTime)
            .Must((cmd, end) => cmd.CrossesMidnight || end > cmd.StartTime)
            .WithMessage("End time must be after start time unless the shift crosses midnight.");

        RuleFor(x => x.EndTime)
            .NotEqual(x => x.StartTime)
            .WithMessage("End time must differ from start time.");
    }
}
