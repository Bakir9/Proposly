using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Proposly.Application.Abstractions;

namespace Proposly.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        RegisterHandlers(services, assembly, typeof(ICommandHandler<>));
        RegisterHandlers(services, assembly, typeof(ICommandHandler<,>));
        RegisterHandlers(services, assembly, typeof(IQueryHandler<,>));

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly, Type openInterfaceType)
    {
        var registrations = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openInterfaceType)
                .Select(i => (Implementation: t, Interface: i)));

        foreach (var (implementation, @interface) in registrations)
        {
            services.AddScoped(@interface, implementation);
        }
    }
}
