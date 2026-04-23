using Proposly.Application.Abstractions;
using Proposly.Application.ProjectManagement.Responses;
using Proposly.Domain.ProjectManagement.Repositories;

namespace Proposly.Application.ProjectManagement.Queries.GetBurndown;

public sealed class GetBurndownQueryHandler : IQueryHandler<GetBurndownQuery, BurndownResponse?>
{
    private readonly IProjectRepository _repository;

    public GetBurndownQueryHandler(IProjectRepository repository) => _repository = repository;

    public async Task<BurndownResponse?> HandleAsync(GetBurndownQuery query, CancellationToken ct = default)
    {
        var project = await _repository.GetByIdAsync(query.ProjectId, ct);
        if (project is null) return null;

        var tasks = project.Tasks.ToList();
        var totalTasks = tasks.Count;

        var startDate = project.StartDate;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var endDate = project.Deadline.HasValue && project.Deadline.Value < today
            ? project.Deadline.Value
            : today;

        if (endDate < startDate) endDate = startDate;

        var totalDays = (endDate.ToDateTime(TimeOnly.MinValue) - startDate.ToDateTime(TimeOnly.MinValue)).Days;

        var actual = new List<BurndownDataPoint>();
        var ideal = new List<BurndownDataPoint>();

        var current = startDate;
        var dayIndex = 0;
        while (current <= endDate)
        {
            var day = current;
            var remaining = tasks.Count(t => t.CompletedAt is null || t.CompletedAt.Value > day);
            actual.Add(new BurndownDataPoint(day, remaining));

            var idealRemaining = totalDays == 0
                ? 0
                : (int)Math.Round(totalTasks * (1.0 - (double)dayIndex / totalDays));
            ideal.Add(new BurndownDataPoint(day, Math.Max(0, idealRemaining)));

            current = current.AddDays(1);
            dayIndex++;
        }

        return new BurndownResponse(totalTasks, startDate, endDate, actual, ideal);
    }
}
