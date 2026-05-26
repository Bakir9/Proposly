using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Application.Admin.Queries.GetAllCompanies;

public sealed class GetAllCompaniesQueryHandler : IQueryHandler<GetAllCompaniesQuery, List<CompanyAdminSummary>>
{
    private readonly ICompanyRepository _companyRepository;

    public GetAllCompaniesQueryHandler(ICompanyRepository companyRepository)
        => _companyRepository = companyRepository;

    public async Task<List<CompanyAdminSummary>> HandleAsync(GetAllCompaniesQuery query, CancellationToken ct = default)
    {
        var rows = await _companyRepository.GetAllWithCountsAsync(ct);

        return rows.Select(r => new CompanyAdminSummary(
            r.Company.Id,
            r.Company.Name,
            r.Company.Status.ToString(),
            r.Company.PlanTier.ToString(),
            r.Company.MaxUsers,
            r.Company.MaxProjects,
            r.Company.PlanExpiresAt,
            r.UserCount,
            r.ProjectCount,
            r.Company.CreatedAt)).ToList();
    }
}
