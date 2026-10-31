using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Shared.Exceptions;

namespace Proposly.Application.ProjectManagement.Commands.AddTaskComment;

public sealed class AddTaskCommentCommandHandler : ICommandHandler<AddTaskCommentCommand, Guid>
{
    private readonly IProjectRepository _projects;
    private readonly IUserRepository _users;
    private readonly ICurrentUserService _currentUser;

    public AddTaskCommentCommandHandler(IProjectRepository projects, IUserRepository users, ICurrentUserService currentUser)
    {
        _projects = projects;
        _users = users;
        _currentUser = currentUser;
    }

    public async Task<Guid> HandleAsync(AddTaskCommentCommand command, CancellationToken ct = default)
    {
        var project = await _projects.GetByIdForWriteAsync(command.ProjectId, ct)
            ?? throw AppException.NotFound("project.not_found", $"Project {command.ProjectId} not found.");

        var user = await _users.GetByIdAsync(_currentUser.UserId, ct)
            ?? throw AppException.NotFound("current_user.not_found", "Current user not found.");

        var authorName = $"{user.FirstName} {user.LastName}";
        var comment = project.AddTaskComment(command.TaskId, _currentUser.UserId, authorName, command.Body);

        await _projects.UpdateAsync(project, ct);
        return comment.Id;
    }
}
