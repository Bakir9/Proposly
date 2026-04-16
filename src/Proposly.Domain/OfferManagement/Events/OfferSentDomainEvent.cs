using Proposly.Shared.Primitives;

namespace Proposly.Domain.OfferManagement.Events;

public sealed record OfferSentDomainEvent(Guid OfferId, Guid CompanyId, Guid ClientId) : IDomainEvent;
