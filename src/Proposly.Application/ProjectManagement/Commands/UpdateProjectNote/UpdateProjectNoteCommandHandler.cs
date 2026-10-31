using Proposly.Application.Abstractions;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Shared.Exceptions;

namespace Proposly.Application.ProjectManagement.Commands.UpdateProjectNote;

public sealed class UpdateProjectNoteCommandHandler : ICommandHandler<UpdateProjectNoteCommand>
{
    private readonly IProjectRepository _repository;

    public UpdateProjectNoteCommandHandler(IProjectRepository repository) => _repository = repository;

    public async Task HandleAsync(UpdateProjectNoteCommand command, CancellationToken ct = default)
    {
        var project = await _repository.GetByIdForWriteAsync(command.ProjectId, ct)
            ?? throw AppException.NotFound("project.not_found", $"Project {command.ProjectId} not found.");

        project.UpdateNote(command.NoteId, command.Title, command.Content);
        await _repository.UpdateAsync(project, ct);
    }
}
