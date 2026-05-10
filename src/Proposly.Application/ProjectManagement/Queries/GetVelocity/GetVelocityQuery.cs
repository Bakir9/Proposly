using Proposly.Application.Abstractions;
using Proposly.Application.ProjectManagement.Responses;

namespace Proposly.Application.ProjectManagement.Queries.GetVelocity;

public sealed record GetVelocityQuery(Guid ProjectId) : IQuery<VelocityResponse?>;
