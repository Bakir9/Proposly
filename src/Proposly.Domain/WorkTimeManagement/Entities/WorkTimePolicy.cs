using Proposly.Shared.Interfaces;
using Proposly.Shared.Primitives;

namespace Proposly.Domain.WorkTimeManagement.Entities;

/// <summary>
/// A company's working time agreement as it applied from <see cref="ValidFrom"/>: the statutory
/// limits, the tiered break minimums, and the flexitime bounds on the carried balance.
/// <para>
/// Versioned rather than mutable, so a change never rewrites what an already-approved month was
/// judged against. Company-wide, so deliberately NOT <see cref="IUserOwnedEntity"/> — every
/// employee must be able to read the rules that apply to them.
/// </para>
/// <para>
/// Consolidates the spec's "Working Time Rule Set" and "Flexitime Agreement Settings": both come
/// from the same written agreement, are company-scoped, and are versioned by the same date.
/// </para>
/// </summary>
public sealed class WorkTimePolicy : AggregateRoot<Guid>, ITenantEntity, IAuditableEntity
{
    private readonly List<BreakRule> _breakRules = [];

    private WorkTimePolicy() { } // For EF Core

    private WorkTimePolicy(
        Guid id,
        Guid companyId,
        DateOnly validFrom,
        string jurisdiction,
        string? holidayRegionCode,
        decimal maxHoursPerDay,
        decimal maxHoursPerWeek,
        int averagingWindowWeeks,
        decimal maxAverageHoursPerWeek,
        decimal minDailyRestHours,
        decimal minWeeklyRestHours,
        decimal? surplusCapHours,
        decimal? deficitFloorHours)
        : base(id)
    {
        CompanyId = companyId;
        ValidFrom = validFrom;
        Jurisdiction = jurisdiction;
        HolidayRegionCode = holidayRegionCode;
        MaxHoursPerDay = maxHoursPerDay;
        MaxHoursPerWeek = maxHoursPerWeek;
        AveragingWindowWeeks = averagingWindowWeeks;
        MaxAverageHoursPerWeek = maxAverageHoursPerWeek;
        MinDailyRestHours = minDailyRestHours;
        MinWeeklyRestHours = minWeeklyRestHours;
        SurplusCapHours = surplusCapHours;
        DeficitFloorHours = deficitFloorHours;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static WorkTimePolicy Create(
        Guid companyId,
        DateOnly validFrom,
        string jurisdiction,
        string? holidayRegionCode,
        decimal maxHoursPerDay,
        decimal maxHoursPerWeek,
        int averagingWindowWeeks,
        decimal maxAverageHoursPerWeek,
        decimal minDailyRestHours,
        decimal minWeeklyRestHours,
        decimal? surplusCapHours,
        decimal? deficitFloorHours)
    {
        if (string.IsNullOrWhiteSpace(jurisdiction))
            throw new ArgumentException("Jurisdiction is required.", nameof(jurisdiction));

        if (maxHoursPerDay is <= 0 or > 24)
            throw new ArgumentException("Daily maximum must be between 0 and 24 hours.", nameof(maxHoursPerDay));

        if (maxHoursPerWeek <= 0)
            throw new ArgumentException("Weekly maximum must be positive.", nameof(maxHoursPerWeek));

        if (averagingWindowWeeks <= 0)
            throw new ArgumentException("Averaging window must be at least one week.", nameof(averagingWindowWeeks));

        if (surplusCapHours is < 0)
            throw new ArgumentException("Surplus cap cannot be negative.", nameof(surplusCapHours));

        if (deficitFloorHours is > 0)
            throw new ArgumentException("Deficit floor must be zero or negative.", nameof(deficitFloorHours));

        return new WorkTimePolicy(
            Guid.NewGuid(), companyId, validFrom, jurisdiction, holidayRegionCode,
            maxHoursPerDay, maxHoursPerWeek, averagingWindowWeeks, maxAverageHoursPerWeek,
            minDailyRestHours, minWeeklyRestHours, surplusCapHours, deficitFloorHours);
    }

    public Guid CompanyId { get; private set; }
    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }

    /// <summary>Seeds the defaults — "AT", "DE", …</summary>
    public string Jurisdiction { get; private set; } = string.Empty;

    /// <summary>
    /// Country plus subdivision where the country has them, e.g. "AT-4". Public holidays differ by
    /// subdivision in Austria and Germany. Stored now; consumed by the deferred holiday import.
    /// </summary>
    public string? HolidayRegionCode { get; private set; }

    public decimal MaxHoursPerDay { get; private set; }
    public decimal MaxHoursPerWeek { get; private set; }
    public int AveragingWindowWeeks { get; private set; }
    public decimal MaxAverageHoursPerWeek { get; private set; }
    public decimal MinDailyRestHours { get; private set; }
    public decimal MinWeeklyRestHours { get; private set; }

    /// <summary>Null means the carried surplus is unbounded, and is reported as such.</summary>
    public decimal? SurplusCapHours { get; private set; }

    /// <summary>Null means the carried deficit is unbounded. Stored as zero or negative.</summary>
    public decimal? DeficitFloorHours { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<BreakRule> BreakRules => _breakRules.AsReadOnly();

    public BreakRule AddBreakRule(decimal aboveHours, int minBreakMinutes)
    {
        if (_breakRules.Any(r => r.AboveHours == aboveHours))
            throw new InvalidOperationException(
                $"A break tier for above {aboveHours} hours already exists.");

        var rule = BreakRule.Create(Id, aboveHours, minBreakMinutes);
        _breakRules.Add(rule);
        UpdatedAt = DateTime.UtcNow;
        return rule;
    }

    /// <summary>
    /// The minimum break required for a day of the given length, or 0 when no tier applies.
    /// The highest matching tier wins, so Germany's 45 minutes above 9h supersedes its 30 above 6h.
    /// </summary>
    public int RequiredBreakMinutes(decimal workedHours)
        => _breakRules
            .Where(r => workedHours > r.AboveHours)
            .OrderByDescending(r => r.AboveHours)
            .Select(r => r.MinBreakMinutes)
            .FirstOrDefault();

    /// <summary>Closes this version the day before its successor takes effect.</summary>
    public void CloseAt(DateOnly successorValidFrom)
    {
        if (successorValidFrom <= ValidFrom)
            throw new InvalidOperationException(
                "A new policy version must take effect after the current one.");

        ValidTo = successorValidFrom.AddDays(-1);
        UpdatedAt = DateTime.UtcNow;
    }

    public bool AppliesOn(DateOnly date)
        => date >= ValidFrom && (ValidTo is null || date <= ValidTo);
}
