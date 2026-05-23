using Proposly.Application.Abstractions;
using Proposly.Application.OfferManagement.Responses;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.OfferManagement.Repositories;
using Proposly.Domain.OfferManagement.Services;

namespace Proposly.Application.OfferManagement.Queries.GetVatPreview;

public sealed class GetVatPreviewQueryHandler : IQueryHandler<GetVatPreviewQuery, VatPreviewResponse?>
{
    private readonly IClientRepository _clientRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ICurrentUserService _currentUser;

    public GetVatPreviewQueryHandler(
        IClientRepository clientRepository,
        ICompanyRepository companyRepository,
        ICurrentUserService currentUser)
    {
        _clientRepository = clientRepository;
        _companyRepository = companyRepository;
        _currentUser = currentUser;
    }

    public async Task<VatPreviewResponse?> HandleAsync(GetVatPreviewQuery query, CancellationToken ct = default)
    {
        var client = await _clientRepository.GetByIdAsync(query.ClientId, ct);
        if (client is null) return null;

        var company = await _companyRepository.GetByIdAsync(_currentUser.CompanyId, ct);
        if (company is null) return null;

        var calc = new VatCalculator(
            company.CompanyCountry,
            company.IsVatRegistered,
            company.IsVatExempt,
            company.DefaultVatRate,
            company.VatExemptReason);

        var result = calc.Calculate(client);

        return new VatPreviewResponse(result.Rate, result.Type.ToString(), result.Label, result.Note, result.IsExempt);
    }
}
