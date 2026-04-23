using Proposly.Application.Abstractions;

namespace Proposly.Application.ProjectManagement.Commands.EditTaskComment;

public record EditTaskCommentCommand(Guid ProjectId, Guid TaskId, Guid CommentId, string Body) : ICommand;
