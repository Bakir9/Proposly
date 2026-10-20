using Proposly.Application.Abstractions;
using Proposly.Domain.HrManagement.Repositories;

namespace Proposly.Application.Hr.Commands.DeleteWorkerNote;

public sealed class DeleteWorkerNoteCommandHandler(
    IEmployeeProfileRepository profiles) : ICommandHandler<DeleteWorkerNoteCommand>
{
    public async Task HandleAsync(DeleteWorkerNoteCommand command, CancellationToken ct = default)
    {
        var profile = await profiles.GetByUserIdAsync(command.UserId, ct)
            ?? throw new InvalidOperationException($"Worker {command.UserId} has no HR profile.");

        profile.RemoveNote(command.NoteId);
        await profiles.SaveChangesAsync(ct);
    }
}
