namespace Proposly.Domain.WorkTimeManagement.Enums;

/// <summary>
/// Why a day is not worked. The distinction matters because the two cost the employee
/// differently: a public holiday is free, a company shutdown normally comes out of the
/// employee's own vacation entitlement.
/// </summary>
public enum NonWorkingDayKind
{
    /// <summary>A statutory holiday. Never consumes vacation entitlement.</summary>
    PublicHoliday,

    /// <summary>A company shutdown or bridge day. Consumes entitlement by default.</summary>
    CompanyClosure
}
