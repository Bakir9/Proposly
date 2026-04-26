using FluentValidation;

namespace Proposly.Application.UserManagement.Commands.InviteUser;

public sealed class InviteUserCommandValidator : AbstractValidator<InviteUserCommand>
{
    private static readonly string[] ValidRoles = ["Admin", "Member"];

    public InviteUserCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Role).NotEmpty().Must(r => ValidRoles.Contains(r))
            .WithMessage("Role must be 'Admin' or 'Member'.");
    }
}
