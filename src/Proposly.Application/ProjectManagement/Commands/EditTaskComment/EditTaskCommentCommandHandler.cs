using Proposly.Application.Abstractions;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Shared.Exceptions;

namespace Proposly.Application.ProjectManagement.Commands.EditTaskComment;

public sealed class EditTaskCommentCommandHandler : ICommandHandler<EditTaskCommentCommand>
{
    private readonly IProjectRepository _projects;
    private readonly ICurrentUserService _currentUser;

    public EditTaskCommentCommandHandler(IProjectRepository projects, ICurrentUserService currentUser)
    {
        _projects = projects;
        _currentUser = currentUser;
    }

    public async Task HandleAsync(EditTaskCommentCommand command, CancellationToken ct = default)
    {
        var project = await _projects.GetByIdForWriteAsync(command.ProjectId, ct)
            ?? throw AppException.NotFound("project.not_found", $"Project {command.ProjectId} not found.");

        project.EditTaskComment(command.TaskId, command.CommentId, _currentUser.UserId, command.Body);

        await _projects.UpdateAsync(project, ct);
    }
}
