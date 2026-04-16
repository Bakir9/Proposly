using Proposly.Shared.Primitives;

namespace Proposly.Domain.OfferManagement.Events;

public sealed record OfferCreatedDomainEvent(Guid OfferId, Guid CompanyId) : IDomainEvent;
