using Proposly.Application.Abstractions;

namespace Proposly.Application.ProjectManagement.Commands.DeleteTaskComment;

public record DeleteTaskCommentCommand(Guid ProjectId, Guid TaskId, Guid CommentId) : ICommand;
