using Proposly.Shared.Interfaces;
using Proposly.Shared.Primitives;

namespace Proposly.Domain.WorkTimeManagement.Entities;

/// <summary>
/// One employee's vacation position for one year: what they are owed, what carried over, and what
/// they have used.
/// <para>
/// Carried-over days do not expire automatically — an administrator adjusts the figure if company
/// policy requires forfeiture, rather than the system silently dropping days.
/// </para>
/// </summary>
public sealed class AbsenceEntitlement : AggregateRoot<Guid>, ITenantEntity, IAuditableEntity, IUserOwnedEntity
{
    private AbsenceEntitlement() { } // For EF Core

    private AbsenceEntitlement(
        Guid id, Guid companyId, Guid userId, int year, decimal entitledDays, decimal carriedOverDays)
        : base(id)
    {
        CompanyId = companyId;
        UserId = userId;
        Year = year;
        EntitledDays = entitledDays;
        CarriedOverDays = carriedOverDays;
        UsedDays = 0m;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static AbsenceEntitlement Create(
        Guid companyId, Guid userId, int year, decimal entitledDays, decimal carriedOverDays = 0m)
    {
        if (year is < 2000 or > 2100)
            throw new ArgumentOutOfRangeException(nameof(year), "Year is outside the supported range.");

        if (entitledDays < 0)
            throw new ArgumentException("Entitled days cannot be negative.", nameof(entitledDays));

        if (carriedOverDays < 0)
            throw new ArgumentException("Carried-over days cannot be negative.", nameof(carriedOverDays));

        return new AbsenceEntitlement(Guid.NewGuid(), companyId, userId, year, entitledDays, carriedOverDays);
    }

    public Guid CompanyId { get; private set; }
    public Guid UserId { get; private set; }
    public int Year { get; private set; }
    public decimal EntitledDays { get; private set; }
    public decimal CarriedOverDays { get; private set; }
    public decimal UsedDays { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public decimal RemainingDays => EntitledDays + CarriedOverDays - UsedDays;

    /// <summary>
    /// Draws down entitlement on approval. Refuses to go past the remaining balance, so an
    /// approver cannot accidentally grant days the employee does not have.
    /// </summary>
    public void Consume(decimal days)
    {
        if (days < 0)
            throw new ArgumentException("Days consumed cannot be negative.", nameof(days));

        if (days > RemainingDays)
            throw new InvalidOperationException(
                $"Only {RemainingDays} day(s) remain; {days} day(s) requested.");

        UsedDays += days;
        Touch();
    }

    /// <summary>Returns days to the balance when an absence is cancelled.</summary>
    public void Release(decimal days)
    {
        if (days < 0)
            throw new ArgumentException("Days released cannot be negative.", nameof(days));

        UsedDays = Math.Max(0m, UsedDays - days);
        Touch();
    }

    public void SetEntitlement(decimal entitledDays, decimal carriedOverDays)
    {
        if (entitledDays < 0)
            throw new ArgumentException("Entitled days cannot be negative.", nameof(entitledDays));

        if (carriedOverDays < 0)
            throw new ArgumentException("Carried-over days cannot be negative.", nameof(carriedOverDays));

        EntitledDays = entitledDays;
        CarriedOverDays = carriedOverDays;
        Touch();
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
