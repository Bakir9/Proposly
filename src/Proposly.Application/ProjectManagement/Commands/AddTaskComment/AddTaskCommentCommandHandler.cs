using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.ProjectManagement.Repositories;

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
        var project = await _projects.GetByIdAsync(command.ProjectId, ct)
            ?? throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        var user = await _users.GetByIdAsync(_currentUser.UserId, ct)
            ?? throw new InvalidOperationException("Current user not found.");

        var authorName = $"{user.FirstName} {user.LastName}";
        var comment = project.AddTaskComment(command.TaskId, _currentUser.UserId, authorName, command.Body);

        await _projects.UpdateAsync(project, ct);
        return comment.Id;
    }
}
