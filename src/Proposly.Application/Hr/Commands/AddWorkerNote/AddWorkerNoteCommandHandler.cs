using Proposly.Application.Abstractions;
using Proposly.Application.Hr.Responses;
using Proposly.Domain.HrManagement.Entities;
using Proposly.Domain.HrManagement.Repositories;

namespace Proposly.Application.Hr.Commands.AddWorkerNote;

public sealed class AddWorkerNoteCommandHandler(
    IEmployeeProfileRepository profiles,
    ICurrentUserService currentUser) : ICommandHandler<AddWorkerNoteCommand, WorkerNoteResponse>
{
    public async Task<WorkerNoteResponse> HandleAsync(AddWorkerNoteCommand command, CancellationToken ct = default)
    {
        var profile = await profiles.GetByUserIdAsync(command.UserId, ct);
        if (profile is null)
        {
            profile = EmployeeProfile.Create(currentUser.CompanyId, command.UserId);
            await profiles.AddAsync(profile, ct);
        }

        var note = profile.AddNote(command.Tag, command.Text);
        await profiles.SaveChangesAsync(ct);

        return new WorkerNoteResponse(note.Id, note.Tag, note.Text, DateOnly.FromDateTime(note.CreatedAt), note.Pinned);
    }
}
