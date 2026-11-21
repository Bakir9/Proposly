using Proposly.Application.Abstractions;
using Proposly.Application.ProjectManagement.Responses;
using Proposly.Domain.ProjectManagement.Repositories;

namespace Proposly.Application.ProjectManagement.Queries.GetTaskTemplates;

public sealed class GetTaskTemplatesQueryHandler(
    ITaskTemplateRepository templates) : IQueryHandler<GetTaskTemplatesQuery, IReadOnlyList<TaskTemplateResponse>>
{
    public async Task<IReadOnlyList<TaskTemplateResponse>> HandleAsync(GetTaskTemplatesQuery query, CancellationToken ct = default)
    {
        var all = await templates.GetAllAsync(ct);

        return all
            .Select(t => new TaskTemplateResponse(
                t.Id,
                t.Name,
                t.Description,
                t.TotalEstimatedHours(),
                t.Steps
                    .OrderBy(s => s.SortOrder)
                    .Select(s => new TaskTemplateStepResponse(s.Id, s.Title, s.EstimatedHours, s.SortOrder))
                    .ToList()))
            .ToList();
    }
}
