using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Proposly.Application.Abstractions;

namespace Proposly.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddValidatorsFromAssembly(assembly, lifetime: ServiceLifetime.Scoped);

        RegisterCommandHandlers(services, assembly, typeof(ICommandHandler<>));
        RegisterCommandHandlers(services, assembly, typeof(ICommandHandler<,>));
        RegisterDirect(services, assembly, typeof(IQueryHandler<,>));
        RegisterDirect(services, assembly, typeof(IDomainEventHandler<>));

        return services;
    }

    private static void RegisterCommandHandlers(IServiceCollection services, Assembly assembly, Type openInterfaceType)
    {
        var openDecoratorType = openInterfaceType == typeof(ICommandHandler<>)
            ? typeof(ValidatingCommandHandler<>)
            : typeof(ValidatingCommandHandler<,>);

        var registrations = GetRegistrations(assembly, openInterfaceType);

        foreach (var (implementation, @interface) in registrations)
        {
            services.AddScoped(implementation);

            var decoratorType = openDecoratorType.MakeGenericType(@interface.GetGenericArguments());
            var concreteImpl = implementation;
            services.AddScoped(@interface, sp =>
            {
                var inner = sp.GetRequiredService(concreteImpl);
                return ActivatorUtilities.CreateInstance(sp, decoratorType, inner);
            });
        }
    }

    private static void RegisterDirect(IServiceCollection services, Assembly assembly, Type openInterfaceType)
    {
        foreach (var (implementation, @interface) in GetRegistrations(assembly, openInterfaceType))
            services.AddScoped(@interface, implementation);
    }

    private static IEnumerable<(Type Implementation, Type Interface)> GetRegistrations(Assembly assembly, Type openInterfaceType)
        => assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericType)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openInterfaceType)
                .Select(i => (Implementation: t, Interface: i)));
}
