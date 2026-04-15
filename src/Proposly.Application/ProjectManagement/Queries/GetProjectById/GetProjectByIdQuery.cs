using Proposly.Application.Abstractions;
using Proposly.Application.ProjectManagement.Responses;

namespace Proposly.Application.ProjectManagement.Queries.GetProjectById;

public record GetProjectByIdQuery(Guid ProjectId) : IQuery<ProjectDetailResponse?>;
