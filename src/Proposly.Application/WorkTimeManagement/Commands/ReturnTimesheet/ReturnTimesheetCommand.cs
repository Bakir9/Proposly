using Proposly.Application.Abstractions;

namespace Proposly.Application.WorkTimeManagement.Commands.ReturnTimesheet;

/// <summary>Sends a submitted month back to Draft so the employee can correct it.</summary>
public sealed record ReturnTimesheetCommand(Guid TimesheetId, string? Reason = null) : ICommand;
