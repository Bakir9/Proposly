using Proposly.Application.Abstractions;
using Proposly.Application.Settings.Responses;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.ProjectManagement.Repositories;

namespace Proposly.Application.Settings.Queries.GetCompanySettings;

public sealed class GetCompanySettingsQueryHandler : IQueryHandler<GetCompanySettingsQuery, CompanySettingsResponse?>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetCompanySettingsQueryHandler(
        ICompanyRepository companyRepository,
        IUserRepository userRepository,
        IProjectRepository projectRepository,
        ICurrentUserService currentUserService)
    {
        _companyRepository = companyRepository;
        _userRepository = userRepository;
        _projectRepository = projectRepository;
        _currentUserService = currentUserService;
    }

    public async Task<CompanySettingsResponse?> HandleAsync(GetCompanySettingsQuery query, CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetByIdAsync(_currentUserService.CompanyId, cancellationToken);
        if (company is null) return null;

        var userCount = await _userRepository.CountInvitedByCompanyIdAsync(_currentUserService.CompanyId, cancellationToken);
        var projectCount = await _projectRepository.CountByCompanyIdAsync(_currentUserService.CompanyId, cancellationToken);

        return new CompanySettingsResponse(
            company.FiscalYearStartMonth,
            company.CompanyEmail,
            company.CompanyPhone,
            company.CompanyStreet,
            company.CompanyCity,
            company.CompanyPostalCode,
            company.CompanyCountry,
            company.IsVatRegistered,
            company.CompanyVatNumber,
            company.DefaultVatRate,
            company.IsVatExempt,
            company.VatExemptReason,
            company.PlanTier,
            company.MaxUsers,
            company.MaxProjects,
            company.PlanExpiresAt,
            userCount,
            projectCount);
    }
}
