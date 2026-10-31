using Proposly.Application.Abstractions;
using Proposly.Application.ProjectManagement.Responses;
using Proposly.Domain.ProjectManagement.Repositories;

namespace Proposly.Application.ProjectManagement.Queries.GetProjectFile;

public sealed class GetProjectFileQueryHandler(
    IProjectRepository projects,
    IProjectFileRepository files,
    IFileStorage fileStorage) : IQueryHandler<GetProjectFileQuery, ProjectFileContentResponse?>
{
    public async Task<ProjectFileContentResponse?> HandleAsync(GetProjectFileQuery query, CancellationToken ct = default)
    {
        // Member visibility: a caller who cannot see the project cannot download its files.
        var project = await projects.GetByIdForWriteAsync(query.ProjectId, ct);
        if (project is null) return null;

        var file = await files.GetByIdAsync(query.FileId, ct);
        if (file is null || file.ProjectId != query.ProjectId) return null;

        var content = await fileStorage.OpenAsync(file.StorageKey, ct);
        if (content is null) return null;

        return new ProjectFileContentResponse(file.FileName, file.ContentType, content);
    }
}
