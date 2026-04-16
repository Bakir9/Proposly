using Proposly.Shared.Primitives;

namespace Proposly.Domain.OfferManagement.Events;

public sealed record OfferAcceptedDomainEvent(Guid OfferId, Guid CompanyId, Guid ClientId) : IDomainEvent;
