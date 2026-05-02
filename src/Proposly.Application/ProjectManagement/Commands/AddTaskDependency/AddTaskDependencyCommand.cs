using Proposly.Application.Abstractions;

namespace Proposly.Application.ProjectManagement.Commands.AddTaskDependency;

public record AddTaskDependencyCommand(Guid ProjectId, Guid TaskId, Guid BlockingTaskId) : ICommand;
