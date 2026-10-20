using Proposly.Application.Abstractions;
using Proposly.Application.Hr.Commands.UploadWorkerFile;
using Proposly.Application.Hr.Responses;

namespace Proposly.Application.Hr.Queries.GetWorkerFile;

/// <summary>Downloads a worker's photo, CV, or one document (DocumentId required then).</summary>
public sealed record GetWorkerFileQuery(
    Guid UserId,
    WorkerFileKind Kind,
    Guid? DocumentId) : IQuery<HrFileResponse?>;
