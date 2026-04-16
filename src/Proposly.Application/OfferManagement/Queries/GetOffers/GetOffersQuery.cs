using Proposly.Application.Abstractions;
using Proposly.Application.OfferManagement.Responses;
using Proposly.Domain.OfferManagement.Enums;

namespace Proposly.Application.OfferManagement.Queries.GetOffers;

public record GetOffersQuery(OfferStatus? Status = null) : IQuery<IReadOnlyList<OfferSummaryResponse>>;
