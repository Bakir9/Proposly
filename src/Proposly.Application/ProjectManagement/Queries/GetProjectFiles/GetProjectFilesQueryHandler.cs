using Proposly.Application.Abstractions;
using Proposly.Application.Chat.Services;
using Proposly.Application.ProjectManagement.Responses;
using Proposly.Domain.Chat.Repositories;
using Proposly.Domain.ProjectManagement.Repositories;

namespace Proposly.Application.ProjectManagement.Queries.GetProjectFiles;

/// <summary>
/// Aggregates the Files page: direct uploads ("Upload") and the project channel's shared
/// attachments ("Discussion"). Project visibility is enforced by the repository filter —
/// a member who is not on the project gets null → 404.
/// </summary>
public sealed class GetProjectFilesQueryHandler(
    IProjectRepository projects,
    IProjectFileRepository files,
    IConversationRepository conversations,
    IChatReadService chatRead,
    ICurrentUserService currentUser) : IQueryHandler<GetProjectFilesQuery, IReadOnlyList<ProjectFileResponse>?>
{
    public async Task<IReadOnlyList<ProjectFileResponse>?> HandleAsync(GetProjectFilesQuery query, CancellationToken ct = default)
    {
        var project = await projects.GetByIdForWriteAsync(query.ProjectId, ct);
        if (project is null) return null;

        var isAdmin = currentUser.CanViewAllEmployees;

        var uploads = (await files.GetByProjectIdAsync(query.ProjectId, ct))
            .Select(f => new ProjectFileResponse(
                f.Id, f.FileName, f.ContentType, f.SizeBytes,
                "Upload", f.UploadedByName, f.CreatedAt,
                CanDelete: isAdmin || f.UploadedByUserId == currentUser.UserId));

        var result = uploads.ToList();

        var conversation = await conversations.GetByProjectIdAsync(query.ProjectId, ct);
        if (conversation is not null)
        {
            var shared = await chatRead.GetConversationFilesAsync(conversation.Id, ct);
            result.AddRange(shared.Select(a => new ProjectFileResponse(
                a.Id, a.FileName, a.ContentType, a.SizeBytes,
                "Discussion", a.UploaderName, a.CreatedAt,
                CanDelete: false)));
        }

        return result.OrderByDescending(f => f.UploadedAt).ToList();
    }
}
