using Proposly.Application.Abstractions;
using Proposly.Application.Hr.Responses;

namespace Proposly.Application.Hr.Queries.GetWorker;

public sealed record GetWorkerQuery(Guid UserId) : IQuery<WorkerResponse?>;
