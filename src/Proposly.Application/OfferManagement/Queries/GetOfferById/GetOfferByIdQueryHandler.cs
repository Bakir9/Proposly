using Proposly.Application.Abstractions;
using Proposly.Application.OfferManagement.Responses;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.OfferManagement.Repositories;

namespace Proposly.Application.OfferManagement.Queries.GetOfferById;

public sealed class GetOfferByIdQueryHandler : IQueryHandler<GetOfferByIdQuery, OfferDetailResponse?>
{
    private readonly IOfferRepository _offerRepository;
    private readonly IClientRepository _clientRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ICurrentUserService _currentUser;

    public GetOfferByIdQueryHandler(
        IOfferRepository offerRepository,
        IClientRepository clientRepository,
        ICompanyRepository companyRepository,
        ICurrentUserService currentUser)
    {
        _offerRepository = offerRepository;
        _clientRepository = clientRepository;
        _companyRepository = companyRepository;
        _currentUser = currentUser;
    }

    public async Task<OfferDetailResponse?> HandleAsync(GetOfferByIdQuery query, CancellationToken ct = default)
    {
        var offer = await _offerRepository.GetByIdAsync(query.OfferId, ct);
        if (offer is null) return null;

        var client = await _clientRepository.GetByIdAsync(offer.ClientId, ct);
        var company = await _companyRepository.GetByIdAsync(_currentUser.CompanyId, ct);

        return new OfferDetailResponse(
            offer.Id,
            offer.ClientId,
            client?.Name ?? "Unknown",
            client?.ContactPerson,
            client?.Email,
            client?.Phone,
            client?.Address?.Street,
            client?.Address?.City,
            client?.Address?.PostalCode,
            client?.Address?.Country,
            client?.VatNumber,
            company?.Name ?? string.Empty,
            company?.CompanyEmail,
            company?.CompanyPhone,
            company?.CompanyStreet,
            company?.CompanyCity,
            company?.CompanyPostalCode,
            offer.Title,
            offer.Notes,
            offer.Status,
            offer.CalculateSubtotal().Amount,
            offer.DiscountPercent,
            offer.CalculateDiscountAmount().Amount,
            offer.CalculateVatBase().Amount,
            offer.CalculateVatAmount().Amount,
            offer.CalculateTotal().Amount,
            offer.VatRate,
            offer.VatType.ToString(),
            offer.VatLabel,
            offer.VatNote,
            offer.IsVatExempt,
            offer.Currency,
            offer.ValidUntil,
            offer.SentAt,
            offer.CreatedAt,
            offer.Items.Select(i => new OfferItemResponse(
                i.Id,
                i.Description,
                i.Quantity,
                i.UnitPrice.Amount,
                i.LineTotal.Amount,
                i.UnitPrice.Currency)).ToList());
    }
}
