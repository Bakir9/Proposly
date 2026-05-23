using Proposly.Application.Abstractions;
using Proposly.Application.Settings.Responses;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Application.Settings.Queries.GetCompanySettings;

public sealed class GetCompanySettingsQueryHandler : IQueryHandler<GetCompanySettingsQuery, CompanySettingsResponse?>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetCompanySettingsQueryHandler(ICompanyRepository companyRepository, ICurrentUserService currentUserService)
    {
        _companyRepository = companyRepository;
        _currentUserService = currentUserService;
    }

    public async Task<CompanySettingsResponse?> HandleAsync(GetCompanySettingsQuery query, CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetByIdAsync(_currentUserService.CompanyId, cancellationToken);
        return company is null ? null : new CompanySettingsResponse(
            company.FiscalYearStartMonth,
            company.CompanyCountry,
            company.IsVatRegistered,
            company.CompanyVatNumber,
            company.DefaultVatRate,
            company.IsVatExempt,
            company.VatExemptReason);
    }
}
