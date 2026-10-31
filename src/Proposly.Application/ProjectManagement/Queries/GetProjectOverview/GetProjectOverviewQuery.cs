using Proposly.Application.Abstractions;
using Proposly.Application.ProjectManagement.Responses;

namespace Proposly.Application.ProjectManagement.Queries.GetProjectOverview;

public sealed record GetProjectOverviewQuery(Guid ProjectId) : IQuery<ProjectOverviewResponse?>;
