using Microsoft.EntityFrameworkCore;
using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.ProjectManagement.Entities;
using Proposly.Shared.Interfaces;

namespace Proposly.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Project> Projects => Set<Project>();

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Apply global tenant filter to every entity that implements ITenantEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            var method = typeof(AppDbContext)
                .GetMethod(nameof(SetTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .MakeGenericMethod(entityType.ClrType);

            method.Invoke(null, [modelBuilder, _currentUserService]);
        }

        base.OnModelCreating(modelBuilder);
    }

    private static void SetTenantFilter<TEntity>(ModelBuilder modelBuilder, ICurrentUserService currentUserService)
        where TEntity : class, ITenantEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.CompanyId == currentUserService.CompanyId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(cancellationToken);
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
