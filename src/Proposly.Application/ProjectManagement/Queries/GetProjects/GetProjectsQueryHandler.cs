using Proposly.Application.Abstractions;
using Proposly.Application.ProjectManagement.Responses;
using Proposly.Domain.ProjectManagement.Enums;
using Proposly.Domain.ProjectManagement.Repositories;

namespace Proposly.Application.ProjectManagement.Queries.GetProjects;

public sealed class GetProjectsQueryHandler : IQueryHandler<GetProjectsQuery, IReadOnlyList<ProjectSummaryResponse>>
{
    private readonly IProjectRepository _repository;

    public GetProjectsQueryHandler(IProjectRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<ProjectSummaryResponse>> HandleAsync(GetProjectsQuery query, CancellationToken ct = default)
    {
        var projects = await _repository.GetAllAsync(ct);

        return projects.Select(p => new ProjectSummaryResponse(
            p.Id,
            p.Name,
            p.ClientName,
            p.Status,
            p.Budget.Amount,
            p.Budget.Currency,
            p.StartDate,
            p.Deadline,
            p.CreatedAt,
            p.Members.Count,
            p.Tasks.Count(t => t.Status == ProjectTaskStatus.Done),
            p.Tasks.Count)).ToList();
    }
}
