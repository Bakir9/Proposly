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
        EmploymentType employmentType,
        decimal weeklyHours,
        WeekDays workingDays,
        decimal annualVacationDays,
        bool isAllIn,
        decimal? overtimeLumpSumHours)
        : base(id)
    {
        CompanyId = companyId;
        UserId = userId;
        ValidFrom = validFrom;
        EmploymentType = employmentType;
        WeeklyHours = weeklyHours;
        WorkingDays = workingDays;
        AnnualVacationDays = annualVacationDays;
        IsAllIn = isAllIn;
        OvertimeLumpSumHours = overtimeLumpSumHours;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static EmploymentTerms Create(
        Guid companyId,
        Guid userId,
        DateOnly validFrom,
        decimal weeklyHours,
        WeekDays workingDays,
        decimal annualVacationDays,
        EmploymentType employmentType = EmploymentType.FullTime,
        bool isAllIn = false,
        decimal? overtimeLumpSumHours = null)
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

        if (overtimeLumpSumHours is < 0)
            throw new ArgumentException(
                "An overtime lump sum cannot be negative.", nameof(overtimeLumpSumHours));

        if (overtimeLumpSumHours is > 200)
            throw new ArgumentException(
                "An overtime lump sum of more than 200 hours a month is implausible.",
                nameof(overtimeLumpSumHours));

        // An all-in salary already covers every additional hour, so a separate lump sum on top of
        // it is contradictory: the same overtime would be compensated twice over.
        if (isAllIn && overtimeLumpSumHours is > 0)
            throw new InvalidOperationException(
                "An all-in contract already covers overtime, so it cannot also carry an " +
                "overtime lump sum. Use one or the other.");

        return new EmploymentTerms(
            Guid.NewGuid(), companyId, userId, validFrom, employmentType,
            weeklyHours, workingDays, annualVacationDays, isAllIn, overtimeLumpSumHours);
    }

    public Guid CompanyId { get; private set; }
    public Guid UserId { get; private set; }
    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }
    /// <summary>
    /// The kind of contract. Recorded for the record and shown on reports; it suggests a starting
    /// figure for weekly hours in the UI but never constrains the value stored here.
    /// </summary>
    public EmploymentType EmploymentType { get; private set; }

    public decimal WeeklyHours { get; private set; }
    public WeekDays WorkingDays { get; private set; }
    public decimal AnnualVacationDays { get; private set; }

    /// <summary>
    /// All-in agreement: salary covers additional hours, so a surplus is recorded and reported but
    /// never carried forward as time owed. A shortfall still carries — the salary covers overtime,
    /// not undertime.
    /// </summary>
    public bool IsAllIn { get; private set; }

    /// <summary>
    /// Überstundenpauschale — overtime hours per month already paid by a lump sum. Surplus up to
    /// this figure is absorbed rather than banked; only hours beyond it carry forward.
    /// Null means no lump sum.
    /// </summary>
    public decimal? OvertimeLumpSumHours { get; private set; }

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
