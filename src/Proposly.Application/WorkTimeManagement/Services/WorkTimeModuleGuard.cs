using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Shared.Exceptions;

namespace Proposly.Application.WorkTimeManagement.Services;

/// <summary>
/// Plan gate for the module. Applied on write paths only — a company that drops below Pro keeps
/// read access to records it already has, but cannot create new ones.
/// </summary>
internal static class WorkTimeModuleGuard
{
    public static async Task EnsureEnabledAsync(
        ICompanyRepository companies, Guid companyId, CancellationToken ct)
    {
        var company = await companies.GetByIdAsync(companyId, ct)
            ?? throw AppException.NotFound("company.not_found", $"Company {companyId} not found.");

        if (!company.HasWorkTimeModule())
            throw AppException.Forbidden("plan.worktime_module_required",
                "Working time management is available on the Pro and Business plans.");
    }
}
