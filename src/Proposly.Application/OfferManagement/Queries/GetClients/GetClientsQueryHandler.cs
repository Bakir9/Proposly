using Proposly.Application.Abstractions;
using Proposly.Application.OfferManagement.Responses;
using Proposly.Domain.OfferManagement.Repositories;

namespace Proposly.Application.OfferManagement.Queries.GetClients;

public sealed class GetClientsQueryHandler : IQueryHandler<GetClientsQuery, IReadOnlyList<ClientSummaryResponse>>
{
    private readonly IClientRepository _repository;

    public GetClientsQueryHandler(IClientRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<ClientSummaryResponse>> HandleAsync(GetClientsQuery query, CancellationToken ct = default)
    {
        var clients = await _repository.GetAllAsync(ct);
        return clients.Select(c => new ClientSummaryResponse(
            c.Id,
            c.Name,
            c.ContactPerson,
            c.Email,
            c.Phone,
            c.Website,
            c.Address?.Street,
            c.Address?.City,
            c.Address?.PostalCode,
            c.Address?.Country,
            c.Currency,
            c.VatNumber,
            c.Status)).ToList();
    }
}
