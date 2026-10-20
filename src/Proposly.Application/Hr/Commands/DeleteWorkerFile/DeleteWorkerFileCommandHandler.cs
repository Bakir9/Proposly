using Proposly.Application.Abstractions;
using Proposly.Application.Hr.Commands.UploadWorkerFile;
using Proposly.Domain.HrManagement.Repositories;

namespace Proposly.Application.Hr.Commands.DeleteWorkerFile;

public sealed class DeleteWorkerFileCommandHandler(
    IEmployeeProfileRepository profiles,
    IFileStorage fileStorage) : ICommandHandler<DeleteWorkerFileCommand>
{
    public async Task HandleAsync(DeleteWorkerFileCommand command, CancellationToken ct = default)
    {
        var profile = await profiles.GetByUserIdAsync(command.UserId, ct)
            ?? throw new InvalidOperationException($"Worker {command.UserId} has no HR profile.");

        switch (command.Kind)
        {
            case WorkerFileKind.Photo:
                if (profile.PhotoStorageKey is not null)
                    await fileStorage.DeleteAsync(profile.PhotoStorageKey, ct);
                profile.SetPhoto(null);
                break;

            case WorkerFileKind.Cv:
                if (profile.CvStorageKey is not null)
                    await fileStorage.DeleteAsync(profile.CvStorageKey, ct);
                profile.RemoveCvFile();
                break;

            default:
                var documentId = command.DocumentId
                    ?? throw new InvalidOperationException("DocumentId is required to delete a document.");
                var removed = profile.RemoveDocument(documentId);
                await fileStorage.DeleteAsync(removed.StorageKey, ct);
                break;
        }

        await profiles.SaveChangesAsync(ct);
    }
}
