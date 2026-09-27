using Proposly.Application.Abstractions;

namespace Proposly.Application.WorkTimeManagement.Commands.ApproveTimesheet;

/// <summary>
/// Approver sign-off on a submitted month.
/// <para>
/// Where the month has outstanding working time breaches, <paramref name="AcknowledgeBreaches"/>
/// must be true: an approver has to see and accept them explicitly, and the acknowledgement is
/// recorded against them.
/// </para>
/// </summary>
public sealed record ApproveTimesheetCommand(
    Guid TimesheetId,
    bool AcknowledgeBreaches = false) : ICommand;
