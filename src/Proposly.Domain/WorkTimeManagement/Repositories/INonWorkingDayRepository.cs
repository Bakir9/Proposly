using Proposly.Domain.WorkTimeManagement.Entities;

namespace Proposly.Domain.WorkTimeManagement.Repositories;

public interface INonWorkingDayRepository
{
    Task<NonWorkingDay?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<NonWorkingDay>> GetForRangeAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>Used to refuse a second entry on a date the company already marks non-working.</summary>
    Task<NonWorkingDay?> GetByDateAsync(DateOnly date, CancellationToken ct = default);

    Task AddAsync(NonWorkingDay day, CancellationToken ct = default);
    Task RemoveAsync(NonWorkingDay day, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
