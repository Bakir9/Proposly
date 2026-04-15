using Proposly.Application.Abstractions;
using Proposly.Application.ProjectManagement.Responses;

namespace Proposly.Application.ProjectManagement.Queries.GetProjects;

public record GetProjectsQuery : IQuery<IReadOnlyList<ProjectSummaryResponse>>;
