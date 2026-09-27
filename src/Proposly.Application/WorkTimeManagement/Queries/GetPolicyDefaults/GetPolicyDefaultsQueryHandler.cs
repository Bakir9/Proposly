using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;
using Proposly.Domain.WorkTimeManagement.Services;

namespace Proposly.Application.WorkTimeManagement.Queries.GetPolicyDefaults;

public sealed class GetPolicyDefaultsQueryHandler
    : IQueryHandler<GetPolicyDefaultsQuery, WorkTimePolicyResponse?>
{
    private readonly ICurrentUserService _currentUser;
    private readonly TimeProvider _clock;

    public GetPolicyDefaultsQueryHandler(ICurrentUserService currentUser, TimeProvider clock)
    {
        _currentUser = currentUser;
        _clock = clock;
    }

    public Task<WorkTimePolicyResponse?> HandleAsync(
        GetPolicyDefaultsQuery query, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime);

        var policy = WorkTimePolicyDefaults.For(
            query.Jurisdiction, _currentUser.CompanyId, validFrom: today);

        // Nothing is persisted here — the response is a form seed the admin reviews and submits.
        return Task.FromResult(policy?.ToResponse());
    }
}
