using Proposly.Application.Abstractions;
using Proposly.Domain.ProjectManagement.Repositories;

namespace Proposly.Application.ProjectManagement.Commands.DeleteTaskComment;

public sealed class DeleteTaskCommentCommandHandler : ICommandHandler<DeleteTaskCommentCommand>
{
    private readonly IProjectRepository _projects;
    private readonly ICurrentUserService _currentUser;

    public DeleteTaskCommentCommandHandler(IProjectRepository projects, ICurrentUserService currentUser)
    {
        _projects = projects;
        _currentUser = currentUser;
    }

    public async Task HandleAsync(DeleteTaskCommentCommand command, CancellationToken ct = default)
    {
        var project = await _projects.GetByIdAsync(command.ProjectId, ct)
            ?? throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        var isAdmin = _currentUser.Role is "Owner" or "Admin";
        project.DeleteTaskComment(command.TaskId, command.CommentId, _currentUser.UserId, isAdmin);

        await _projects.UpdateAsync(project, ct);
    }
}
