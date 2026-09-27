using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;
using Proposly.Application.WorkTimeManagement.Services;
using Proposly.Domain.CompanyManagement.Repositories;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Queries.GetPendingAbsences;

public sealed class GetPendingAbsencesQueryHandler
    : IQueryHandler<GetPendingAbsencesQuery, IReadOnlyList<AbsenceResponse>>
{
    private readonly IAbsenceRepository _absences;
    private readonly IUserRepository _users;

    public GetPendingAbsencesQueryHandler(IAbsenceRepository absences, IUserRepository users)
    {
        _absences = absences;
        _users = users;
    }

    public async Task<IReadOnlyList<AbsenceResponse>> HandleAsync(
        GetPendingAbsencesQuery query, CancellationToken ct = default)
    {
        var pending = await _absences.GetPendingAsync(ct);
        if (pending.Count == 0) return [];

        var names = await _users.ResolveNamesAsync(ct);

        return pending
            .Select(a => a.ToResponse(names.GetValueOrDefault(a.UserId, "Unknown")))
            .ToList();
    }
}
