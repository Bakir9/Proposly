using Proposly.Application.Abstractions;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Shared.Exceptions;

namespace Proposly.Application.ProjectManagement.Commands.DeleteProjectFile;

public sealed class DeleteProjectFileCommandHandler(
    IProjectFileRepository files,
    IFileStorage fileStorage,
    ICurrentUserService currentUser) : ICommandHandler<DeleteProjectFileCommand>
{
    public async Task HandleAsync(DeleteProjectFileCommand command, CancellationToken ct = default)
    {
        var file = await files.GetByIdAsync(command.FileId, ct);
        if (file is null || file.ProjectId != command.ProjectId)
            throw AppException.NotFound("project_file.not_found", $"File {command.FileId} not found.");

        // Own uploads, or anyone with the admin view — same rule the response's CanDelete reports.
        if (file.UploadedByUserId != currentUser.UserId && !currentUser.CanViewAllEmployees)
            throw AppException.Forbidden("project_file.not_owner", "Only the uploader or an admin can delete this file.");

        await fileStorage.DeleteAsync(file.StorageKey, ct);
        await files.RemoveAsync(file, ct);
    }
}
