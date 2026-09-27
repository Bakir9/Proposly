using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;
using Proposly.Application.WorkTimeManagement.Services;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Queries.GetAbsences;

public sealed class GetAbsencesQueryHandler
    : IQueryHandler<GetAbsencesQuery, IReadOnlyList<AbsenceResponse>>
{
    private readonly IAbsenceRepository _absences;
    private readonly IUserRepository _users;

    public GetAbsencesQueryHandler(IAbsenceRepository absences, IUserRepository users)
    {
        _absences = absences;
        _users = users;
    }

    public async Task<IReadOnlyList<AbsenceResponse>> HandleAsync(
        GetAbsencesQuery query, CancellationToken ct = default)
    {
        var absences = await _absences.GetForYearAsync(query.Year, query.Status, ct);
        if (absences.Count == 0) return [];

        var names = await _users.ResolveNamesAsync(ct);

        return absences
            .Select(a => a.ToResponse(
                names.GetValueOrDefault(a.UserId, "Unknown"),
                a.ApproverId.HasValue ? names.GetValueOrDefault(a.ApproverId.Value) : null))
            .ToList();
    }
}
