using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Shared.Exceptions;

namespace Proposly.Application.ProjectManagement.Commands.AddProjectNote;

public sealed class AddProjectNoteCommandHandler : ICommandHandler<AddProjectNoteCommand, Guid>
{
    private readonly IProjectRepository _projects;
    private readonly IUserRepository _users;
    private readonly ICurrentUserService _currentUser;

    public AddProjectNoteCommandHandler(IProjectRepository projects, IUserRepository users, ICurrentUserService currentUser)
    {
        _projects = projects;
        _users = users;
        _currentUser = currentUser;
    }

    public async Task<Guid> HandleAsync(AddProjectNoteCommand command, CancellationToken ct = default)
    {
        var project = await _projects.GetByIdForWriteAsync(command.ProjectId, ct)
            ?? throw AppException.NotFound("project.not_found", $"Project {command.ProjectId} not found.");

        var user = await _users.GetByIdAsync(_currentUser.UserId, ct)
            ?? throw AppException.NotFound("current_user.not_found", "Current user not found.");

        var note = project.AddNote(command.Title, command.Content, _currentUser.UserId, $"{user.FirstName} {user.LastName}");
        await _projects.UpdateAsync(project, ct);
        return note.Id;
    }
}
