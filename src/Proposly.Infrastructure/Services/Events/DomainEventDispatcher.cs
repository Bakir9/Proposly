using Microsoft.Extensions.DependencyInjection;
using Proposly.Application.Abstractions;
using Proposly.Shared.Primitives;

namespace Proposly.Infrastructure.Services.Events;

public sealed class DomainEventDispatcher(IServiceProvider sp) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken ct = default)
    {
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
        var handlers = sp.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;
            await (Task)method.Invoke(handler, [domainEvent, ct])!;
        }
    }
}
