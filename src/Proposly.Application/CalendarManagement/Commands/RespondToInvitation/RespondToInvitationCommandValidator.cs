using FluentValidation;

namespace Proposly.Application.CalendarManagement.Commands.RespondToInvitation;

public sealed class RespondToInvitationCommandValidator : AbstractValidator<RespondToInvitationCommand>
{
    public RespondToInvitationCommandValidator()
    {
        RuleFor(x => x.Action).Must(a => a == "Accept" || a == "Decline")
            .WithMessage("Action must be 'Accept' or 'Decline'.");
    }
}
