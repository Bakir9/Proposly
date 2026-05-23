using Proposly.Application.Abstractions;
using Proposly.Application.OfferManagement.Responses;
using Proposly.Domain.OfferManagement.Repositories;

namespace Proposly.Application.OfferManagement.Queries.GetOfferById;

public sealed class GetOfferByIdQueryHandler : IQueryHandler<GetOfferByIdQuery, OfferDetailResponse?>
{
    private readonly IOfferRepository _offerRepository;
    private readonly IClientRepository _clientRepository;

    public GetOfferByIdQueryHandler(IOfferRepository offerRepository, IClientRepository clientRepository)
    {
        _offerRepository = offerRepository;
        _clientRepository = clientRepository;
    }

    public async Task<OfferDetailResponse?> HandleAsync(GetOfferByIdQuery query, CancellationToken ct = default)
    {
        var offer = await _offerRepository.GetByIdAsync(query.OfferId, ct);
        if (offer is null) return null;

        var client = await _clientRepository.GetByIdAsync(offer.ClientId, ct);
        var clientName = client?.Name ?? "Unknown";

        return new OfferDetailResponse(
            offer.Id,
            offer.ClientId,
            clientName,
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
