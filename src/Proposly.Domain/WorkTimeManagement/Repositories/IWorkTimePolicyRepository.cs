using Proposly.Domain.WorkTimeManagement.Entities;

namespace Proposly.Domain.WorkTimeManagement.Repositories;

public interface IWorkTimePolicyRepository
{
    /// <summary>The version in force on a date, or null when the company has none configured.</summary>
    Task<WorkTimePolicy?> GetEffectiveAsync(DateOnly onDate, CancellationToken ct = default);

    /// <summary>The most recent version, whether or not it is yet in force.</summary>
    Task<WorkTimePolicy?> GetLatestAsync(CancellationToken ct = default);

    Task<IReadOnlyList<WorkTimePolicy>> GetHistoryAsync(CancellationToken ct = default);

    Task AddAsync(WorkTimePolicy policy, CancellationToken ct = default);
    Task UpdateAsync(WorkTimePolicy policy, CancellationToken ct = default);
}
