using Proposly.Application.Abstractions;
using Proposly.Application.Hr.Responses;
using Proposly.Domain.HrManagement.Entities;
using Proposly.Domain.HrManagement.Repositories;

namespace Proposly.Application.Hr.Commands.UploadWorkerFile;

public sealed class UploadWorkerFileCommandHandler(
    IEmployeeProfileRepository profiles,
    IFileStorage fileStorage,
    ICurrentUserService currentUser) : ICommandHandler<UploadWorkerFileCommand, WorkerDocumentResponse?>
{
    public async Task<WorkerDocumentResponse?> HandleAsync(UploadWorkerFileCommand command, CancellationToken ct = default)
    {
        var profile = await profiles.GetByUserIdAsync(command.UserId, ct);
        if (profile is null)
        {
            profile = EmployeeProfile.Create(currentUser.CompanyId, command.UserId);
            await profiles.AddAsync(profile, ct);
        }

        var storageKey = await fileStorage.SaveAsync(command.Content, command.ContentType, ct);

        switch (command.Kind)
        {
            case WorkerFileKind.Photo:
                if (profile.PhotoStorageKey is not null)
                    await fileStorage.DeleteAsync(profile.PhotoStorageKey, ct);
                profile.SetPhoto(storageKey);
                await profiles.SaveChangesAsync(ct);
                return null;

            case WorkerFileKind.Cv:
                if (profile.CvStorageKey is not null)
                    await fileStorage.DeleteAsync(profile.CvStorageKey, ct);
                profile.SetCvFile(command.FileName, command.Content.LongLength, storageKey);
                await profiles.SaveChangesAsync(ct);
                return null;

            default:
                var document = profile.AddDocument(
                    command.Category!, command.FileName, command.ContentType,
                    command.Content.LongLength, storageKey);
                await profiles.SaveChangesAsync(ct);
                return new WorkerDocumentResponse(
                    document.Id, document.Category, document.FileName, document.SizeBytes,
                    DateOnly.FromDateTime(document.CreatedAt));
        }
    }
}
