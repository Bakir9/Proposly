using Proposly.Application.Abstractions;
using Proposly.Application.OfferManagement.Responses;

namespace Proposly.Application.OfferManagement.Queries.GetClientById;

public record GetClientByIdQuery(Guid ClientId) : IQuery<ClientDetailResponse?>;
