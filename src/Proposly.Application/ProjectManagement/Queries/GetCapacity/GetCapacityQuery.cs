using Proposly.Application.Abstractions;
using Proposly.Application.ProjectManagement.Responses;

namespace Proposly.Application.ProjectManagement.Queries.GetCapacity;

public sealed record GetCapacityQuery(Guid ProjectId) : IQuery<CapacityResponse?>;
