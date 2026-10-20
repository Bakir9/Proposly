using Proposly.Application.Abstractions;
using Proposly.Application.Hr.Commands.UploadWorkerFile;

namespace Proposly.Application.Hr.Commands.DeleteWorkerFile;

/// <summary>Removes a worker's photo, CV file, or one document (DocumentId required then).</summary>
public sealed record DeleteWorkerFileCommand(
    Guid UserId,
    WorkerFileKind Kind,
    Guid? DocumentId) : ICommand;
