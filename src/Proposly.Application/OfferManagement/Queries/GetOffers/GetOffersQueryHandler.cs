using Proposly.Application.Abstractions;
using Proposly.Application.OfferManagement.Responses;
using Proposly.Domain.OfferManagement.Repositories;

namespace Proposly.Application.OfferManagement.Queries.GetOffers;

public sealed class GetOffersQueryHandler : IQueryHandler<GetOffersQuery, IReadOnlyList<OfferSummaryResponse>>
{
    private readonly IOfferRepository _offerRepository;
    private readonly IClientRepository _clientRepository;

    public GetOffersQueryHandler(IOfferRepository offerRepository, IClientRepository clientRepository)
    {
        _offerRepository = offerRepository;
        _clientRepository = clientRepository;
    }

    public async Task<IReadOnlyList<OfferSummaryResponse>> HandleAsync(GetOffersQuery query, CancellationToken ct = default)
    {
        var offers = query.Status.HasValue
            ? await _offerRepository.GetByStatusAsync(query.Status.Value, ct)
            : await _offerRepository.GetAllAsync(ct);

        var clientIds = offers.Select(o => o.ClientId).Distinct().ToList();
        var clients = new Dictionary<Guid, string>();
        foreach (var clientId in clientIds)
        {
            var client = await _clientRepository.GetByIdAsync(clientId, ct);
            if (client is not null)
                clients[clientId] = client.Name;
        }

        return offers.Select(o => new OfferSummaryResponse(
            o.Id,
            o.ClientId,
            clients.GetValueOrDefault(o.ClientId, "Unknown"),
            o.Title,
            o.Status,
            o.CalculateSubtotal().Amount,
            o.DiscountPercent,
            o.CalculateDiscountAmount().Amount,
            o.CalculateTotal().Amount,
            o.Currency,
            o.ValidUntil,
            o.CreatedAt)).ToList();
    }
}
