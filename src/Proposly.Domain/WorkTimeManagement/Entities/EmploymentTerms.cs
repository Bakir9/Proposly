using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Shared.Interfaces;
using Proposly.Shared.Primitives;

namespace Proposly.Domain.WorkTimeManagement.Entities;

/// <summary>
/// A dated version of one employee's working arrangement.
/// <para>
/// Versioned rather than mutable: someone dropping from 40 to 30 hours in July must not
/// retroactively rewrite January to June. There are no setters beyond <see cref="CloseAt"/>, so
/// history is immutable at the domain level rather than by validator.
/// </para>
/// </summary>
public sealed class EmploymentTerms : AggregateRoot<Guid>, ITenantEntity, IAuditableEntity, IUserOwnedEntity
{
    private EmploymentTerms() { } // For EF Core

    private EmploymentTerms(
        Guid id,
        Guid companyId,
        Guid userId,
        DateOnly validFrom,
        decimal weeklyHours,
        WeekDays workingDays,
        decimal annualVacationDays)
        : base(id)
    {
        CompanyId = companyId;
        UserId = userId;
        ValidFrom = validFrom;
        WeeklyHours = weeklyHours;
        WorkingDays = workingDays;
        AnnualVacationDays = annualVacationDays;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static EmploymentTerms Create(
        Guid companyId,
        Guid userId,
        DateOnly validFrom,
        decimal weeklyHours,
        WeekDays workingDays,
        decimal annualVacationDays)
    {
        if (weeklyHours is <= 0 or > 60)
            throw new ArgumentException(
                "Weekly hours must be between 0 and 60.", nameof(weeklyHours));

        if (workingDays == WeekDays.None)
            throw new ArgumentException(
                "At least one working weekday is required.", nameof(workingDays));

        if (annualVacationDays is < 0 or > 366)
            throw new ArgumentException(
                "Annual vacation days must be between 0 and 366.", nameof(annualVacationDays));

        return new EmploymentTerms(
            Guid.NewGuid(), companyId, userId, validFrom, weeklyHours, workingDays, annualVacationDays);
    }

    public Guid CompanyId { get; private set; }
    public Guid UserId { get; private set; }
    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }
    public decimal WeeklyHours { get; private set; }
    public WeekDays WorkingDays { get; private set; }
    public decimal AnnualVacationDays { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Contracted hours for one working day — weekly hours spread over the working pattern.
    /// A four-day 32-hour week gives 8 hours a day, not 6.4.
    /// </summary>
    public decimal DailyHours
    {
        get
        {
            var days = WorkingDays.Count();
            return days == 0 ? 0m : Math.Round(WeeklyHours / days, 4, MidpointRounding.AwayFromZero);
        }
    }

    /// <summary>Closes this version the day before its successor takes effect.</summary>
    public void CloseAt(DateOnly successorValidFrom)
    {
        if (successorValidFrom <= ValidFrom)
            throw new InvalidOperationException(
                "New employment terms must take effect after the current version.");

        ValidTo = successorValidFrom.AddDays(-1);
        UpdatedAt = DateTime.UtcNow;
    }

    public bool AppliesOn(DateOnly date)
        => date >= ValidFrom && (ValidTo is null || date <= ValidTo);
}
