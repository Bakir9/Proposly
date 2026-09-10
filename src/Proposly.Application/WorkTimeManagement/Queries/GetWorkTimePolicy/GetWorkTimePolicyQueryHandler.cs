using Proposly.Application.Abstractions;
using Proposly.Application.WorkTimeManagement.Responses;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Queries.GetWorkTimePolicy;

public sealed class GetWorkTimePolicyQueryHandler
    : IQueryHandler<GetWorkTimePolicyQuery, WorkTimePolicyResponse?>
{
    private readonly IWorkTimePolicyRepository _policies;
    private readonly TimeProvider _clock;

    public GetWorkTimePolicyQueryHandler(IWorkTimePolicyRepository policies, TimeProvider clock)
    {
        _policies = policies;
        _clock = clock;
    }

    public async Task<WorkTimePolicyResponse?> HandleAsync(
        GetWorkTimePolicyQuery query, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime);

        // Falls back to the latest version so a policy dated in the future is still visible to the
        // admin who scheduled it, rather than reading as "nothing configured".
        var policy = await _policies.GetEffectiveAsync(today, ct)
                     ?? await _policies.GetLatestAsync(ct);

        if (policy is null) return null;

        var history = (await _policies.GetHistoryAsync(ct))
            .Select(p => new PolicyVersionResponse(p.Id, p.ValidFrom, p.ValidTo))
            .ToList();

        return policy.ToResponse(history);
    }
}
