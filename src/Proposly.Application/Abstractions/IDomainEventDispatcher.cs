using Proposly.Shared.Primitives;

namespace Proposly.Application.Abstractions;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken ct = default);
}
