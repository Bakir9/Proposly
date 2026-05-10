using Microsoft.EntityFrameworkCore;
using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.Notifications;
using Proposly.Domain.OfferManagement.Entities;
using Proposly.Domain.ProjectManagement.Entities;
using Proposly.Shared.Interfaces;
using Proposly.Shared.Primitives;

namespace Proposly.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDomainEventDispatcher _dispatcher;

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ClientNote> ClientNotes => Set<ClientNote>();
    public DbSet<Notification> Notifications => Set<Notification>();

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ICurrentUserService currentUserService,
        IDomainEventDispatcher dispatcher)
        : base(options)
    {
        _currentUserService = currentUserService;
        _dispatcher = dispatcher;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            var method = typeof(AppDbContext)
                .GetMethod(nameof(SetTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .MakeGenericMethod(entityType.ClrType);

            method.Invoke(this, [modelBuilder]);
        }

        base.OnModelCreating(modelBuilder);
    }

    private void SetTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.CompanyId == _currentUserService.CompanyId);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();

        var domainEvents = ChangeTracker.Entries<object>()
            .Select(e => e.Entity)
            .OfType<IHasDomainEvents>()
            .SelectMany(e => e.DomainEvents)
            .ToList();

        foreach (var aggregate in ChangeTracker.Entries<object>()
            .Select(e => e.Entity)
            .OfType<IHasDomainEvents>())
        {
            aggregate.ClearDomainEvents();
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in domainEvents)
            await _dispatcher.DispatchAsync(domainEvent, cancellationToken);

        return result;
    }

    private void UpdateAuditFields()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(nameof(IAuditableEntity.CreatedAt)).CurrentValue = now;
                entry.Property(nameof(IAuditableEntity.UpdatedAt)).CurrentValue = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(IAuditableEntity.UpdatedAt)).CurrentValue = now;
            }
        }
    }
}
