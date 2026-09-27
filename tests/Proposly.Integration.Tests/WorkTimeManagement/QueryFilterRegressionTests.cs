using Microsoft.EntityFrameworkCore;
using Proposly.Application.Abstractions;
using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Infrastructure.Persistence;
using Proposly.Shared.Interfaces;
using Proposly.Shared.Primitives;

namespace Proposly.Integration.Tests.WorkTimeManagement;

/// <summary>
/// Guards the single shared-infrastructure change made by the WorkTimeManagement feature:
/// AppDbContext.OnModelCreating now dispatches between a tenant-only filter and a composed
/// tenant + user filter. Existing entities must be completely unaffected.
///
/// The model is built without touching a database — OnModelCreating runs on first access to
/// context.Model, and Npgsql needs no connection to produce a model.
/// </summary>
public class QueryFilterRegressionTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=proposly_modelonly;Username=none;Password=none")
            .Options;

        return new AppDbContext(options, new FakeCurrentUserService(), new NoOpDispatcher());
    }

    [Fact]
    public void Every_tenant_entity_still_has_a_query_filter()
    {
        using var context = CreateContext();

        var tenantEntities = context.Model.GetEntityTypes()
            .Where(e => typeof(ITenantEntity).IsAssignableFrom(e.ClrType))
            .ToList();

        Assert.NotEmpty(tenantEntities);

        var unfiltered = tenantEntities
            .Where(e => e.GetQueryFilter() is null)
            .Select(e => e.ClrType.Name)
            .ToList();

        Assert.Empty(unfiltered);
    }

    [Fact]
    public void Tenant_only_entities_are_filtered_on_company_alone()
    {
        using var context = CreateContext();

        var leaked = context.Model.GetEntityTypes()
            .Where(e => typeof(ITenantEntity).IsAssignableFrom(e.ClrType))
            .Where(e => !typeof(IUserOwnedEntity).IsAssignableFrom(e.ClrType))
            .Where(e => e.GetQueryFilter()!.ToString()
                         .Contains("UserId", StringComparison.Ordinal))
            .Select(e => e.ClrType.Name)
            .ToList();

        // A tenant-only entity whose filter mentions UserId would mean the composed filter was
        // applied where it should not be — i.e. an existing query changed behaviour.
        Assert.Empty(leaked);
    }

    [Fact]
    public void Only_the_intended_entities_are_user_owned()
    {
        using var context = CreateContext();

        var userOwned = context.Model.GetEntityTypes()
            .Where(e => typeof(IUserOwnedEntity).IsAssignableFrom(e.ClrType))
            .Select(e => e.ClrType.Name)
            .OrderBy(n => n)
            .ToList();

        // Every addition here narrows what some users can see, so the set is asserted explicitly
        // rather than counted. Extend it deliberately as later phases add user-owned entities.
        Assert.Equal(
            new[]
            {
                nameof(AbsenceEntitlement),
                nameof(AbsenceRequest),
                nameof(EmploymentTerms),
                nameof(Timesheet),
            },
            userOwned);
    }

    [Fact]
    public void Company_reference_data_is_not_user_owned()
    {
        using var context = CreateContext();

        // NonWorkingDay and WorkTimePolicy shape every employee's target hours and absence counts,
        // so scoping them to one person would hide the rules from the people they apply to.
        foreach (var type in new[] { typeof(NonWorkingDay), typeof(WorkTimePolicy) })
        {
            var filter = context.Model.FindEntityType(type)!.GetQueryFilter()!.ToString();

            Assert.Contains("CompanyId", filter, StringComparison.Ordinal);
            Assert.DoesNotContain("UserId", filter, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void A_user_owned_entity_is_filtered_on_company_and_employee()
    {
        using var context = CreateContext();

        var filter = context.Model.FindEntityType(typeof(Timesheet))!.GetQueryFilter()!.ToString();

        Assert.Contains("CompanyId", filter, StringComparison.Ordinal);
        Assert.Contains("UserId", filter, StringComparison.Ordinal);

        // Approver access must be expressed inside the predicate. If this disappears, someone has
        // likely reintroduced an IgnoreQueryFilters() bypass, which would drop the company filter
        // as well and allow cross-tenant reads.
        Assert.Contains("CanViewAllEmployees", filter, StringComparison.Ordinal);
    }

    [Fact]
    public void Work_day_entries_are_not_directly_queryable()
    {
        // WorkDayEntry carries no CompanyId, so a DbSet for it would be an unfiltered path around
        // both the tenant and the per-employee filter. It must be reached via Timesheet.Days.
        var dbSets = typeof(AppDbContext)
            .GetProperties()
            .Where(p => p.PropertyType.IsGenericType
                     && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0].Name)
            .ToList();

        Assert.DoesNotContain(nameof(WorkDayEntry), dbSets);
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid CompanyId => Guid.Empty;
        public Guid UserId => Guid.Empty;
        public string Role => string.Empty;
        public bool CanViewAllEmployees => false;
    }

    private sealed class NoOpDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
