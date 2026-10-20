namespace Proposly.Domain.WorkTimeManagement.Enums;

/// <summary>How hours beyond the schedule are compensated.</summary>
public enum OvertimeMode
{
    /// <summary>Overtime is paid out (with the overtime surcharge).</summary>
    PaidOut,

    /// <summary>Overtime is banked as time off in lieu.</summary>
    TimeOff,

    /// <summary>All-in salary — see <c>EmploymentTerms.IsAllIn</c>.</summary>
    AllIn,
}
