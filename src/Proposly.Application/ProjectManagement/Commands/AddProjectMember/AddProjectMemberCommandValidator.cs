using FluentValidation;

namespace Proposly.Application.ProjectManagement.Commands.AddProjectMember;

public sealed class AddProjectMemberCommandValidator : AbstractValidator<AddProjectMemberCommand>
{
    public AddProjectMemberCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Role).NotEmpty().MaximumLength(100);
        RuleFor(x => x.HourlyRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}
