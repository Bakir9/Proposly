using Proposly.Application.Abstractions;
using Proposly.Application.Hr.Commands.UploadWorkerFile;
using Proposly.Application.Hr.Responses;
using Proposly.Domain.HrManagement.Repositories;

namespace Proposly.Application.Hr.Queries.GetWorkerFile;

public sealed class GetWorkerFileQueryHandler(
    IEmployeeProfileRepository profiles,
    IFileStorage fileStorage) : IQueryHandler<GetWorkerFileQuery, HrFileResponse?>
{
    public async Task<HrFileResponse?> HandleAsync(GetWorkerFileQuery query, CancellationToken ct = default)
    {
        var profile = await profiles.GetByUserIdAsync(query.UserId, ct);
        if (profile is null)
            return null;

        var (storageKey, fileName, contentType) = query.Kind switch
        {
            WorkerFileKind.Photo => (profile.PhotoStorageKey, "photo", "image/jpeg"),
            WorkerFileKind.Cv => (profile.CvStorageKey, profile.CvFileName ?? "cv", "application/octet-stream"),
            _ => Document(),
        };

        if (storageKey is null)
            return null;

        var content = await fileStorage.OpenAsync(storageKey, ct);
        return content is null ? null : new HrFileResponse(fileName, contentType, content);

        (string?, string, string) Document()
        {
            var document = profile.Documents.FirstOrDefault(d => d.Id == query.DocumentId);
            return document is null
                ? (null, string.Empty, string.Empty)
                : (document.StorageKey, document.FileName, document.ContentType);
        }
    }
}
