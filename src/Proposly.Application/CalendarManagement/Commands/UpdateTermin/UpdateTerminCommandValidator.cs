using FluentValidation;

namespace Proposly.Application.CalendarManagement.Commands.UpdateTermin;

public sealed class UpdateTerminCommandValidator : AbstractValidator<UpdateTerminCommand>
{
    public UpdateTerminCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        RuleFor(x => x.Location).MaximumLength(300).When(x => x.Location is not null);
    }
}
