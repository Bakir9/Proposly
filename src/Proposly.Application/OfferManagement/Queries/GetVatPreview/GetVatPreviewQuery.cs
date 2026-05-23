using Proposly.Application.Abstractions;
using Proposly.Application.OfferManagement.Responses;

namespace Proposly.Application.OfferManagement.Queries.GetVatPreview;

public record GetVatPreviewQuery(Guid ClientId) : IQuery<VatPreviewResponse?>;
