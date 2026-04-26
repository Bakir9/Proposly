using FluentValidation;

namespace Proposly.Application.ProjectManagement.Commands.LogTime;

public sealed class LogTimeCommandValidator : AbstractValidator<LogTimeCommand>
{
    public LogTimeCommandValidator()
    {
        RuleFor(x => x.HoursWorked).GreaterThan(0).LessThanOrEqualTo(24);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}
