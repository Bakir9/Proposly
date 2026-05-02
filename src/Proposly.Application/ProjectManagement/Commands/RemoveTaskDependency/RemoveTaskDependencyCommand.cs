using Proposly.Application.Abstractions;

namespace Proposly.Application.ProjectManagement.Commands.RemoveTaskDependency;

public record RemoveTaskDependencyCommand(Guid ProjectId, Guid TaskId, Guid BlockingTaskId) : ICommand;
