namespace Proposly.Domain.ProjectManagement.Enums;

/// <summary>
/// How the project earns (or doesn't earn) money — drives profitability, invoicing and wording.
/// </summary>
public enum BillingType
{
    /// <summary>Contract value is the revenue; every hour beyond plan eats margin.</summary>
    FixedPrice = 0,

    /// <summary>Revenue grows with billable hours × member bill rates.</summary>
    TimeAndMaterials = 1,

    /// <summary>No client revenue — cost is tracked against the budget only.</summary>
    Internal = 2,
}
