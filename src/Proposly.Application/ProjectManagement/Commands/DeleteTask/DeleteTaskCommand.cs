using Proposly.Application.Abstractions;

namespace Proposly.Application.ProjectManagement.Commands.DeleteTask;

/// <summary>Deletes a task; a parent task takes its steps with it.</summary>
public sealed record DeleteTaskCommand(Guid ProjectId, Guid TaskId) : ICommand;
