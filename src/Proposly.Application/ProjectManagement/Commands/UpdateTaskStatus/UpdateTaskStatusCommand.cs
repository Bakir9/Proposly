using Proposly.Application.Abstractions;

namespace Proposly.Application.ProjectManagement.Commands.UpdateTaskStatus;

public record UpdateTaskStatusCommand(Guid ProjectId, Guid TaskId, string Status, decimal? ActualHours) : ICommand;
