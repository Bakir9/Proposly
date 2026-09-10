using Proposly.Application.Abstractions;

namespace Proposly.Application.WorkTimeManagement.Commands.LockTimesheet;

/// <summary>Payroll close for an approved month.</summary>
public sealed record LockTimesheetCommand(Guid TimesheetId) : ICommand;
