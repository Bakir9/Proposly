namespace Proposly.Domain.WorkTimeManagement.Enums;

/// <summary>
/// Lifecycle of a monthly timesheet. Day entries may only be changed while Draft.
/// Approved is the approver's sign-off; Locked is the payroll close. Both refuse edits, and both
/// can be reopened back to Draft by an approver.
/// </summary>
public enum TimesheetStatus
{
    Draft,
    Submitted,
    Approved,
    Locked
}
