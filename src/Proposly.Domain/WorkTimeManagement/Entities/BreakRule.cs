using Proposly.Shared.Primitives;

namespace Proposly.Domain.WorkTimeManagement.Entities;

/// <summary>
/// One tier of the minimum-break rule: above <see cref="AboveHours"/> of working time, at least
/// <see cref="MinBreakMinutes"/> of break is required.
/// <para>
/// Austria has one tier (above 6h → 30 min); Germany has two (above 6h → 30 min, above 9h →
/// 45 min). Modelling tiers as rows means a stricter collective agreement is data, not code.
/// </para>
/// </summary>
public sealed class BreakRule : Entity<Guid>
{
    private BreakRule() { } // For EF Core

    private BreakRule(Guid id, Guid workTimePolicyId, decimal aboveHours, int minBreakMinutes)
        : base(id)
    {
        if (aboveHours < 0)
            throw new ArgumentException("Threshold cannot be negative.", nameof(aboveHours));

        if (minBreakMinutes <= 0)
            throw new ArgumentException("A break tier must require some break.", nameof(minBreakMinutes));

        WorkTimePolicyId = workTimePolicyId;
        AboveHours = aboveHours;
        MinBreakMinutes = minBreakMinutes;
    }

    public static BreakRule Create(Guid workTimePolicyId, decimal aboveHours, int minBreakMinutes)
        => new(Guid.NewGuid(), workTimePolicyId, aboveHours, minBreakMinutes);

    public Guid WorkTimePolicyId { get; private set; }

    /// <summary>Working hours above which this tier applies.</summary>
    public decimal AboveHours { get; private set; }

    public int MinBreakMinutes { get; private set; }
}
