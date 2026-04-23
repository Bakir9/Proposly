using Proposly.Application.Abstractions;
using Proposly.Application.ProjectManagement.Responses;

namespace Proposly.Application.ProjectManagement.Queries.GetBurndown;

public sealed record GetBurndownQuery(Guid ProjectId) : IQuery<BurndownResponse?>;
