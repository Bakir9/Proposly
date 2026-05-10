using Proposly.Application.Abstractions;
using Proposly.Application.ProjectManagement.Responses;
using Proposly.Domain.ProjectManagement.Repositories;

namespace Proposly.Application.ProjectManagement.Queries.GetVelocity;

public sealed class GetVelocityQueryHandler : IQueryHandler<GetVelocityQuery, VelocityResponse?>
{
    private readonly IProjectRepository _repository;

    public GetVelocityQueryHandler(IProjectRepository repository) => _repository = repository;

    public async Task<VelocityResponse?> HandleAsync(GetVelocityQuery query, CancellationToken ct = default)
    {
        var project = await _repository.GetByIdAsync(query.ProjectId, ct);
        if (project is null) return null;

        var weeks = project.Tasks
            .Where(t => t.CompletedAt.HasValue)
            .GroupBy(t => GetWeekStart(t.CompletedAt!.Value))
            .OrderBy(g => g.Key)
            .Select(g => new VelocityWeek(
                WeekLabel: $"{g.Key:d MMM} – {g.Key.AddDays(6):d MMM}",
                WeekStart: g.Key,
                TasksCompleted: g.Count(),
                HoursCompleted: g.Sum(t => t.ActualHours ?? t.EstimatedHours ?? 0m)))
            .ToList();

        return new VelocityResponse(
            Weeks: weeks,
            TotalHoursCompleted: weeks.Sum(w => w.HoursCompleted),
            TotalTasksCompleted: weeks.Sum(w => w.TasksCompleted));
    }

    private static DateOnly GetWeekStart(DateOnly date)
    {
        int offset = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-offset);
    }
}
