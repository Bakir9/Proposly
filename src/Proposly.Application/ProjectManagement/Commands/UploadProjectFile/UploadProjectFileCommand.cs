using Proposly.Application.Abstractions;
using Proposly.Application.ProjectManagement.Responses;

namespace Proposly.Application.ProjectManagement.Commands.UploadProjectFile;

public sealed record UploadProjectFileCommand(
    Guid ProjectId,
    string FileName,
    string ContentType,
    byte[] Content) : ICommand<ProjectFileResponse>;
