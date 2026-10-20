using Proposly.Application.Abstractions;
using Proposly.Application.Hr.Responses;

namespace Proposly.Application.Hr.Queries.GetWorkers;

public sealed record GetWorkersQuery : IQuery<IReadOnlyList<WorkerListItemResponse>>;
