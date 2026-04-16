using Proposly.Application.Abstractions;
using Proposly.Application.OfferManagement.Responses;

namespace Proposly.Application.OfferManagement.Queries.GetClients;

public record GetClientsQuery : IQuery<IReadOnlyList<ClientSummaryResponse>>;
