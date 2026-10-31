using FluentValidation;

namespace Proposly.Application.ProjectManagement.Commands.UpdateProjectMember;

public sealed class UpdateProjectMemberCommandValidator : AbstractValidator<UpdateProjectMemberCommand>
{
    public UpdateProjectMemberCommandValidator()
    {
        RuleFor(c => c.ProjectId).NotEmpty();
        RuleFor(c => c.MemberId).NotEmpty();
        RuleFor(c => c.Role).NotEmpty().MaximumLength(100);
        RuleFor(c => c.CostRate).GreaterThanOrEqualTo(0).LessThan(100_000);
        RuleFor(c => c.BillRate).GreaterThanOrEqualTo(0).LessThan(100_000);
        RuleFor(c => c.AllocatedHours).GreaterThanOrEqualTo(0).LessThan(100_000);
    }
}
