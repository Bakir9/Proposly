using Proposly.Shared.Primitives;

namespace Proposly.Domain.OfferManagement.Events;

public sealed record OfferRejectedDomainEvent(Guid OfferId, Guid CompanyId, Guid ClientId) : IDomainEvent;
