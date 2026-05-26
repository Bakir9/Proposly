using Proposly.Application.Abstractions;

namespace Proposly.Application.Admin.Queries.GetAllCompanies;

public record GetAllCompaniesQuery : IQuery<List<CompanyAdminSummary>>;
