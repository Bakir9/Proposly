using Proposly.Application.Abstractions;

namespace Proposly.Application.WorkTimeManagement.Commands.ReopenTimesheet;

/// <summary>Returns an approved or locked month to Draft, recording who reopened it.</summary>
public sealed record ReopenTimesheetCommand(Guid TimesheetId) : ICommand;
