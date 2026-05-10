using Proposly.Application.Abstractions;
using Proposly.Application.ProjectManagement.Responses;
using Proposly.Domain.ProjectManagement.Repositories;

namespace Proposly.Application.ProjectManagement.Queries.GetCapacity;

public sealed class GetCapacityQueryHandler : IQueryHandler<GetCapacityQuery, CapacityResponse?>
{
    private readonly IProjectRepository _repository;

    public GetCapacityQueryHandler(IProjectRepository repository) => _repository = repository;

    public async Task<CapacityResponse?> HandleAsync(GetCapacityQuery query, CancellationToken ct = default)
    {
        var project = await _repository.GetByIdAsync(query.ProjectId, ct);
        if (project is null) return null;

        var members = project.TimeEntries
            .GroupBy(e => e.MemberId)
            .Select(g =>
            {
                var member = project.Members.FirstOrDefault(m => m.Id == g.Key);
                return new MemberCapacity(
                    MemberName: member?.Name ?? "Unknown",
                    HoursLogged: g.Sum(e => e.HoursWorked));
            })
            .OrderByDescending(m => m.HoursLogged)
            .ToList();

        return new CapacityResponse(
            Members: members,
            TotalHoursLogged: members.Sum(m => m.HoursLogged));
    }
}
