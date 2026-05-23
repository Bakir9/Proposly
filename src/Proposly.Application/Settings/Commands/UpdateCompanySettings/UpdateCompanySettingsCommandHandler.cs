using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Application.Settings.Commands.UpdateCompanySettings;

public sealed class UpdateCompanySettingsCommandHandler : ICommandHandler<UpdateCompanySettingsCommand>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCompanySettingsCommandHandler(ICompanyRepository companyRepository, ICurrentUserService currentUserService)
    {
        _companyRepository = companyRepository;
        _currentUserService = currentUserService;
    }

    public async Task HandleAsync(UpdateCompanySettingsCommand command, CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetByIdAsync(_currentUserService.CompanyId, cancellationToken)
            ?? throw new InvalidOperationException("Company not found.");

        company.UpdateSettings(command.FiscalYearStartMonth);
        company.UpdateVatSettings(
            command.CompanyCountry,
            command.IsVatRegistered,
            command.CompanyVatNumber,
            command.DefaultVatRate,
            command.IsVatExempt,
            command.VatExemptReason);
        await _companyRepository.UpdateAsync(company, cancellationToken);
    }
}
