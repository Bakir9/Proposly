using Proposly.Application.Abstractions;
using Proposly.Application.ProjectManagement.Responses;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.ProjectManagement.Entities;
using Proposly.Domain.ProjectManagement.Repositories;
using Proposly.Shared.Exceptions;

namespace Proposly.Application.ProjectManagement.Commands.UploadProjectFile;

public sealed class UploadProjectFileCommandHandler(
    IProjectRepository projects,
    IProjectFileRepository files,
    IUserRepository users,
    IFileStorage fileStorage,
    ICurrentUserService currentUser) : ICommandHandler<UploadProjectFileCommand, ProjectFileResponse>
{
    public async Task<ProjectFileResponse> HandleAsync(UploadProjectFileCommand command, CancellationToken ct = default)
    {
        // Repository reads are member-visibility filtered — a member not on the project gets 404.
        var project = await projects.GetByIdForWriteAsync(command.ProjectId, ct)
            ?? throw AppException.NotFound("project.not_found", $"Project {command.ProjectId} not found.");

        var uploader = await users.GetByIdAsync(currentUser.UserId, ct);
        var storageKey = await fileStorage.SaveAsync(command.Content, command.ContentType, ct);

        var file = ProjectFile.Create(
            currentUser.CompanyId,
            project.Id,
            command.FileName,
            command.ContentType,
            command.Content.LongLength,
            storageKey,
            currentUser.UserId,
            uploader?.FullName ?? "Unknown");

        await files.AddAsync(file, ct);

        return new ProjectFileResponse(
            file.Id, file.FileName, file.ContentType, file.SizeBytes,
            "Upload", file.UploadedByName, DateTime.UtcNow, CanDelete: true);
    }
}
