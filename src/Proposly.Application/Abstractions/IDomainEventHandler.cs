using Proposly.Shared.Primitives;

namespace Proposly.Application.Abstractions;

public interface IDomainEventHandler<TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken ct = default);
}
