using Proposly.Application.Abstractions;
using Proposly.Application.OfferManagement.Responses;

namespace Proposly.Application.OfferManagement.Queries.GetOfferById;

public record GetOfferByIdQuery(Guid OfferId) : IQuery<OfferDetailResponse?>;
