using Proposly.Application.Abstractions;
using Proposly.Application.Hr.Responses;

namespace Proposly.Application.Hr.Commands.UploadWorkerFile;

public enum WorkerFileKind
{
    Photo,
    Cv,
    Document,
}

/// <summary>
/// Stores a worker file (photo, CV or document) via IFileStorage. For documents the response
/// carries the new document row; photo/CV uploads return null.
/// </summary>
public sealed record UploadWorkerFileCommand(
    Guid UserId,
    WorkerFileKind Kind,
    string? Category,
    string FileName,
    string ContentType,
    byte[] Content) : ICommand<WorkerDocumentResponse?>;
