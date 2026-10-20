using Proposly.Domain.WorkTimeManagement.Entities;

namespace Proposly.Domain.WorkTimeManagement.Repositories;

public interface IEmploymentTermsRepository
{
    /// <summary>Every version for one employee, newest first.</summary>
    Task<IReadOnlyList<EmploymentTerms>> GetHistoryAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Versions overlapping a date range. Target-hour calculations resolve terms per day, so a
    /// contract change mid-month needs every version touching the month, not just one.
    /// </summary>
    Task<IReadOnlyList<EmploymentTerms>> GetForRangeAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>The most recent version by start date, whether or not it is yet in force.</summary>
    Task<EmploymentTerms?> GetLatestAsync(Guid userId, CancellationToken ct = default);

    /// <summary>The newest version per employee, for team-wide listings.</summary>
    Task<IReadOnlyList<EmploymentTerms>> GetLatestForCompanyAsync(CancellationToken ct = default);

    Task AddAsync(EmploymentTerms terms, CancellationToken ct = default);

    /// <summary>
    /// Marks a version for deletion without saving. Only for same-day corrections — replacing a
    /// version created earlier today keeps history contiguous without stacking versions.
    /// </summary>
    Task RemoveAsync(EmploymentTerms terms, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
