using Proposly.Application.Abstractions;
using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Application.WorkTimeManagement.Commands.CreateWorkTimePolicy;

public sealed class CreateWorkTimePolicyCommandHandler
    : ICommandHandler<CreateWorkTimePolicyCommand, Guid>
{
    private readonly IWorkTimePolicyRepository _policies;
    private readonly ICurrentUserService _currentUser;

    public CreateWorkTimePolicyCommandHandler(
        IWorkTimePolicyRepository policies,
        ICurrentUserService currentUser)
    {
        _policies = policies;
        _currentUser = currentUser;
    }

    public async Task<Guid> HandleAsync(
        CreateWorkTimePolicyCommand command, CancellationToken ct = default)
    {
        var latest = await _policies.GetLatestAsync(ct);

        if (latest is not null && command.ValidFrom <= latest.ValidFrom)
            throw new InvalidOperationException(
                $"A new policy version must take effect after {latest.ValidFrom:yyyy-MM-dd}.");

        var policy = WorkTimePolicy.Create(
            _currentUser.CompanyId,
            command.ValidFrom,
            command.Jurisdiction,
            command.HolidayRegionCode,
            command.MaxHoursPerDay,
            command.MaxHoursPerWeek,
            command.AveragingWindowWeeks,
            command.MaxAverageHoursPerWeek,
            command.MinDailyRestHours,
            command.MinWeeklyRestHours,
            command.SurplusCapHours,
            command.DeficitFloorHours);

        foreach (var rule in command.BreakRules)
            policy.AddBreakRule(rule.AboveHours, rule.MinBreakMinutes);

        // Closing the predecessor keeps the version ranges contiguous, so "which policy applied in
        // March" stays a single range predicate.
        latest?.CloseAt(command.ValidFrom);

        await _policies.AddAsync(policy, ct);

        return policy.Id;
    }
}
