using Microsoft.EntityFrameworkCore;
using Proposly.Application.Abstractions;
using Proposly.Domain.CalendarManagement.Entities;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.Notifications;
using Proposly.Domain.OfferManagement.Entities;
using Proposly.Domain.ProjectManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Shared.Interfaces;
using Proposly.Shared.Primitives;

namespace Proposly.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDomainEventDispatcher _dispatcher;

    public DbSet<Termin> Termins => Set<Termin>();
    public DbSet<TerminInvitation> TerminInvitations => Set<TerminInvitation>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ClientNote> ClientNotes => Set<ClientNote>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Timesheet> Timesheets => Set<Timesheet>();
    public DbSet<WorkTimePolicy> WorkTimePolicies => Set<WorkTimePolicy>();
    public DbSet<AbsenceRequest> AbsenceRequests => Set<AbsenceRequest>();
    public DbSet<AbsenceEntitlement> AbsenceEntitlements => Set<AbsenceEntitlement>();
    public DbSet<EmploymentTerms> EmploymentTerms => Set<EmploymentTerms>();
    public DbSet<NonWorkingDay> NonWorkingDays => Set<NonWorkingDay>();

    // WorkDayEntry, TimesheetBreach and BreakRule are deliberately NOT exposed as DbSets. It is a child of Timesheet and carries
    // no CompanyId, so a direct query on it would bypass both the tenant and the per-employee
    // filter. Reach day entries through Timesheet.Days; EF still maps the type via that
    // navigation and WorkDayEntryConfiguration.

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

            // Entities that also belong to a single employee get the composed tenant + user filter.
            // Everything else keeps the tenant-only filter it has always had, unchanged.
            var filterMethod = typeof(IUserOwnedEntity).IsAssignableFrom(entityType.ClrType)
                ? nameof(SetTenantAndUserFilter)
                : nameof(SetTenantFilter);

            var method = typeof(AppDbContext)
                .GetMethod(filterMethod, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
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

    /// <summary>
    /// Tenant isolation ANDed with per-employee isolation, as a single predicate.
    /// <para>
    /// EF replaces rather than combines a second HasQueryFilter call on the same entity, so the two
    /// rules are composed here. Approver access is expressed inside the predicate rather than via
    /// IgnoreQueryFilters(), because that would drop the company filter too and allow cross-tenant
    /// reads. Never bypass this filter to reach another employee's rows.
    /// </para>
    /// </summary>
    private void SetTenantAndUserFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantEntity, IUserOwnedEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
            e.CompanyId == _currentUserService.CompanyId
            && (e.UserId == _currentUserService.UserId || _currentUserService.CanViewAllEmployees));
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
