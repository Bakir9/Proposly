using FluentValidation;

namespace Proposly.Application.WorkTimeManagement.Commands.DeleteWorkDay;

public sealed class DeleteWorkDayCommandValidator : AbstractValidator<DeleteWorkDayCommand>
{
    public DeleteWorkDayCommandValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);

        RuleFor(x => x.Date)
            .Must((cmd, date) => date.Year == cmd.Year && date.Month == cmd.Month)
            .WithMessage("Date must fall within the timesheet's year and month.");
    }
}
