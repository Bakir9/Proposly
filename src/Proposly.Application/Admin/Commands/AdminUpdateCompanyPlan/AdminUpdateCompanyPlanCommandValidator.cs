using FluentValidation;
using Proposly.Domain.CompanyManagement.Enums;

namespace Proposly.Application.Admin.Commands.AdminUpdateCompanyPlan;

public sealed class AdminUpdateCompanyPlanCommandValidator : AbstractValidator<AdminUpdateCompanyPlanCommand>
{
    public AdminUpdateCompanyPlanCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();

        RuleFor(x => x.PlanTier)
            .NotEmpty()
            .Must(t => Enum.TryParse<PlanTier>(t, ignoreCase: true, out _))
            .WithMessage($"PlanTier must be one of: {string.Join(", ", Enum.GetNames<PlanTier>())}.");

        RuleFor(x => x.MaxUsers)
            .GreaterThanOrEqualTo(1)
            .When(x => x.MaxUsers.HasValue)
            .WithMessage("MaxUsers must be at least 1.");

        RuleFor(x => x.MaxProjects)
            .GreaterThanOrEqualTo(1)
            .When(x => x.MaxProjects.HasValue)
            .WithMessage("MaxProjects must be at least 1.");
    }
}
