using FluentValidation;

namespace Proposly.Application.Hr.Commands.InviteWorker;

public sealed class InviteWorkerCommandValidator : AbstractValidator<InviteWorkerCommand>
{
    public InviteWorkerCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Role)
            .Must(r => r is "Admin" or "Member")
            .WithMessage("Role must be 'Admin' or 'Member'.");
    }
}
