using Proposly.Application.Abstractions;

namespace Proposly.Application.WorkTimeManagement.Commands.ApproveAbsence;

public sealed record ApproveAbsenceCommand(Guid AbsenceId) : ICommand;
