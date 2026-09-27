using Proposly.Application.Abstractions;

namespace Proposly.Application.WorkTimeManagement.Commands.CancelAbsence;

public sealed record CancelAbsenceCommand(Guid AbsenceId) : ICommand;
