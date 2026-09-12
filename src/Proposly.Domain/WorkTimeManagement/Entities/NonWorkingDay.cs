using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Shared.Interfaces;
using Proposly.Shared.Primitives;

namespace Proposly.Domain.WorkTimeManagement.Entities;

/// <summary>
/// A company-level day on which work is not expected.
/// <para>
/// Company reference data, so deliberately NOT <see cref="IUserOwnedEntity"/> — every employee
/// must be able to read the calendar that shapes their own target hours and absence counts.
/// </para>
/// </summary>
public sealed class NonWorkingDay : AggregateRoot<Guid>, ITenantEntity, IAuditableEntity
{
    private NonWorkingDay() { } // For EF Core

    private NonWorkingDay(
        Guid id,
        Guid companyId,
        DateOnly date,
        string name,
        NonWorkingDayKind kind,
        bool consumesVacation,
        EntrySource source)
        : base(id)
    {
        CompanyId = companyId;
        Date = date;
        Name = name;
        Kind = kind;
        ConsumesVacation = consumesVacation;
        Source = source;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// <paramref name="consumesVacation"/> defaults by kind when not given: false for a public
    /// holiday, true for a company closure.
    /// </summary>
    public static NonWorkingDay Create(
        Guid companyId,
        DateOnly date,
        string name,
        NonWorkingDayKind kind,
        bool? consumesVacation = null,
        EntrySource source = EntrySource.Manual)
    {
        Validate(name, kind, consumesVacation ?? DefaultConsumesVacation(kind));

        return new NonWorkingDay(
            Guid.NewGuid(), companyId, date, name.Trim(), kind,
            consumesVacation ?? DefaultConsumesVacation(kind), source);
    }

    public Guid CompanyId { get; private set; }

    /// <summary>Immutable — delete and recreate to move a day, so the unique index stays honest.</summary>
    public DateOnly Date { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public NonWorkingDayKind Kind { get; private set; }

    /// <summary>
    /// Whether the day costs the employee a vacation day. Always false for a public holiday; true
    /// by default for a closure, overridable for a bridge day the company grants outright.
    /// </summary>
    public bool ConsumesVacation { get; private set; }

    /// <summary>Always Manual for now; holiday import is a later increment.</summary>
    public EntrySource Source { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public void Update(string name, NonWorkingDayKind kind, bool consumesVacation)
    {
        Validate(name, kind, consumesVacation);

        Name = name.Trim();
        Kind = kind;
        ConsumesVacation = consumesVacation;
        UpdatedAt = DateTime.UtcNow;
    }

    private static bool DefaultConsumesVacation(NonWorkingDayKind kind)
        => kind == NonWorkingDayKind.CompanyClosure;

    private static void Validate(string name, NonWorkingDayKind kind, bool consumesVacation)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A non-working day needs a name.", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Name cannot exceed 200 characters.", nameof(name));

        // A statutory holiday costing the employee a vacation day would be wrong in every
        // jurisdiction this module targets, so it is refused rather than merely defaulted.
        if (kind == NonWorkingDayKind.PublicHoliday && consumesVacation)
            throw new InvalidOperationException(
                "A public holiday cannot consume vacation entitlement.");
    }
}
