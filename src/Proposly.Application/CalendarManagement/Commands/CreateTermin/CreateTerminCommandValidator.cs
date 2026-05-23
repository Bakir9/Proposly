using FluentValidation;

namespace Proposly.Application.CalendarManagement.Commands.CreateTermin;

public sealed class CreateTerminCommandValidator : AbstractValidator<CreateTerminCommand>
{
    public CreateTerminCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Start).NotEmpty();
        RuleFor(x => x.End).NotEmpty().GreaterThan(x => x.Start).WithMessage("End must be after start.");
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        RuleFor(x => x.Location).MaximumLength(300).When(x => x.Location is not null);
    }
}
