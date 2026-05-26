namespace Proposly.Application.Admin.Queries.GetAllCompanies;

public record CompanyAdminSummary(
    Guid Id,
    string Name,
    string Status,
    string PlanTier,
    int? MaxUsers,
    int? MaxProjects,
    DateTime? PlanExpiresAt,
    int UserCount,
    int ProjectCount,
    DateTime CreatedAt);
