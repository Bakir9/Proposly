using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Queries.GetEntitlement;

public sealed class GetEntitlementQueryHandler
    : IQueryHandler<GetEntitlementQuery, AbsenceEntitlementResponse?>
{
    private readonly IAbsenceRepository _absences;
    private readonly ICurrentUserService _currentUser;

    public GetEntitlementQueryHandler(
        IAbsenceRepository absences,
        ICurrentUserService currentUser)
    {
        _absences = absences;
        _currentUser = currentUser;
    }

    public async Task<AbsenceEntitlementResponse?> HandleAsync(
        GetEntitlementQuery query, CancellationToken ct = default)
    {
        // A member asking for a colleague's id gets nothing back: the query filter hides the row,
        // so no role check is needed to keep this private.
        var userId = query.UserId ?? _currentUser.UserId;

        var entitlement = await _absences.GetEntitlementAsync(userId, query.Year, ct);

        return entitlement?.ToResponse();
    }
}
