using Proposly.Application.Abstractions;
using Proposly.Application.OfferManagement.Responses;
using Proposly.Domain.OfferManagement.Repositories;

namespace Proposly.Application.OfferManagement.Queries.GetClientById;

public sealed class GetClientByIdQueryHandler : IQueryHandler<GetClientByIdQuery, ClientDetailResponse?>
{
    private readonly IClientRepository _repository;

    public GetClientByIdQueryHandler(IClientRepository repository) => _repository = repository;

    public async Task<ClientDetailResponse?> HandleAsync(GetClientByIdQuery query, CancellationToken ct = default)
    {
        var client = await _repository.GetByIdAsync(query.ClientId, ct);
        if (client is null) return null;

        var notes = client.Notes
            .Select(n => new ClientNoteResponse(n.Id, n.Content, n.AuthorName, n.CreatedAt))
            .ToList();

        return new ClientDetailResponse(
            client.Id,
            client.Name,
            client.ContactPerson,
            client.Email,
            client.Phone,
            client.Website,
            client.Address?.Street,
            client.Address?.City,
            client.Address?.PostalCode,
            client.Address?.Country,
            client.Currency,
            client.VatNumber,
            client.CreatedAt,
            client.Status,
            notes);
    }
}
