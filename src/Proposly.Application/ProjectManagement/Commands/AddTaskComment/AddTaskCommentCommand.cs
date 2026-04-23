using Proposly.Application.Abstractions;

namespace Proposly.Application.ProjectManagement.Commands.AddTaskComment;

public record AddTaskCommentCommand(Guid ProjectId, Guid TaskId, string Body) : ICommand<Guid>;
