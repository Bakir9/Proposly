using Proposly.Application.Abstractions;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.ProjectManagement.Repositories;

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
            ?? throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        var user = await _users.GetByIdAsync(_currentUser.UserId, ct)
            ?? throw new InvalidOperationException("Current user not found.");

        var note = project.AddNote(command.Title, command.Content, _currentUser.UserId, $"{user.FirstName} {user.LastName}");
        await _projects.UpdateAsync(project, ct);
        return note.Id;
    }
}
