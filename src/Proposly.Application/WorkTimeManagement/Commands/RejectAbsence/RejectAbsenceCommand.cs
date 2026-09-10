using Proposly.Application.Abstractions;

namespace Proposly.Application.WorkTimeManagement.Commands.RejectAbsence;

/// <summary>A reason is required — the employee is told why.</summary>
public sealed record RejectAbsenceCommand(Guid AbsenceId, string Reason) : ICommand;
