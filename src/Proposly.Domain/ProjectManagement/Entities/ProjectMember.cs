using Proposly.Shared.Primitives;
using Proposly.Shared.ValueObjects;

namespace Proposly.Domain.ProjectManagement.Entities;

public sealed class ProjectMember : Entity<Guid>
{
    private ProjectMember() { } // For EF Core

    private ProjectMember(Guid id, Guid projectId, Guid userId, string name, string role, Money hourlyRate)
        : base(id)
    {
        ProjectId = projectId;
        UserId = userId;
        Name = name;
        Role = role;
        HourlyRate = hourlyRate;
        // Until distinct rates are set, both sides equal the single legacy rate.
        CostRate = new Money(hourlyRate.Amount, hourlyRate.Currency);
        BillRate = new Money(hourlyRate.Amount, hourlyRate.Currency);
        AllocatedHours = 0;
    }

    public static ProjectMember Create(Guid projectId, Guid userId, string name, string role, Money hourlyRate)
        => new(Guid.NewGuid(), projectId, userId, name, role, hourlyRate);

    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;

    /// <summary>Legacy single rate — superseded by <see cref="CostRate"/> / <see cref="BillRate"/>.</summary>
    public Money HourlyRate { get; private set; } = null!;

    /// <summary>What an hour of this member costs the company.</summary>
    public Money CostRate { get; private set; } = null!;

    /// <summary>What the client pays per hour on time &amp; materials projects.</summary>
    public Money BillRate { get; private set; } = null!;

    /// <summary>Planned hours on this project; 0 = not planned yet.</summary>
    public decimal AllocatedHours { get; private set; }

    public void UpdateRole(string role) => Role = role;

    public void UpdateEngagement(string role, Money costRate, Money billRate, decimal allocatedHours)
    {
        if (allocatedHours < 0)
            throw new InvalidOperationException("Allocated hours cannot be negative.");
        if (costRate.Amount < 0 || billRate.Amount < 0)
            throw new InvalidOperationException("Rates cannot be negative.");

        Role = role;
        CostRate = costRate;
        BillRate = billRate;
        AllocatedHours = allocatedHours;
        // Keep the legacy rate in step with cost so old reads stay sensible.
        HourlyRate = new Money(costRate.Amount, costRate.Currency);
    }
}
