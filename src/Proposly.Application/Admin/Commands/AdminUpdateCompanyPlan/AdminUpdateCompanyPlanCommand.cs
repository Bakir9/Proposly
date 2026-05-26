using Proposly.Application.Abstractions;

namespace Proposly.Application.Admin.Commands.AdminUpdateCompanyPlan;

public record AdminUpdateCompanyPlanCommand(
    Guid CompanyId,
    string PlanTier,
    int? MaxUsers,
    int? MaxProjects,
    DateTime? PlanExpiresAt) : ICommand;
