using Proposly.Shared.Primitives;
using Proposly.Shared.ValueObjects;

namespace Proposly.Domain.HrManagement.Entities;

/// <summary>A recurring extra paid with working time (meal voucher, travel allowance, …).</summary>
public sealed class EmployeeAllowance : Entity<Guid>
{
    public static readonly string[] Units = ["per day", "per month", "per hour", "per km"];

    private EmployeeAllowance() { } // For EF Core

    private EmployeeAllowance(Guid id, Guid employeeProfileId, string name, Money amount, string unit)
        : base(id)
    {
        EmployeeProfileId = employeeProfileId;
        Name = name;
        Amount = amount;
        Unit = unit;
    }

    internal static EmployeeAllowance Create(Guid employeeProfileId, string name, Money amount, string unit)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("An allowance needs a name.");
        if (!Units.Contains(unit))
            throw new InvalidOperationException($"Unknown allowance unit '{unit}'.");
        return new EmployeeAllowance(Guid.NewGuid(), employeeProfileId, name.Trim(), amount, unit);
    }

    public Guid EmployeeProfileId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Money Amount { get; private set; } = null!;
    public string Unit { get; private set; } = string.Empty;
}
