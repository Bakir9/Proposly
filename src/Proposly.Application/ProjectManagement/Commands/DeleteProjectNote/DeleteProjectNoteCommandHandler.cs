using Proposly.Application.Abstractions;
using Proposly.Domain.ProjectManagement.Repositories;

namespace Proposly.Application.ProjectManagement.Commands.DeleteProjectNote;

public sealed class DeleteProjectNoteCommandHandler : ICommandHandler<DeleteProjectNoteCommand>
{
    private readonly IProjectRepository _repository;

    public DeleteProjectNoteCommandHandler(IProjectRepository repository) => _repository = repository;

    public async Task HandleAsync(DeleteProjectNoteCommand command, CancellationToken ct = default)
    {
        var project = await _repository.GetByIdForWriteAsync(command.ProjectId, ct)
            ?? throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        project.DeleteNote(command.NoteId);
        await _repository.UpdateAsync(project, ct);
    }
}
