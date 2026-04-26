using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Proposly.Application.Abstractions;
using Proposly.Shared.Primitives;

namespace Proposly.Infrastructure.Persistence;

/// <summary>
/// Used by dotnet-ef at design time (migrations) only.
/// Not used at runtime — the real AppDbContext is created via DI.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=proposly;Username=postgres;Password=1234")
            .Options;

        return new AppDbContext(options, new DesignTimeCurrentUserService(), new NoOpDispatcher());
    }
}

file sealed class DesignTimeCurrentUserService : ICurrentUserService
{
    public Guid CompanyId => Guid.Empty;
    public Guid UserId => Guid.Empty;
    public string Role => string.Empty;
}

file sealed class NoOpDispatcher : IDomainEventDispatcher
{
    public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken ct = default) => Task.CompletedTask;
}
