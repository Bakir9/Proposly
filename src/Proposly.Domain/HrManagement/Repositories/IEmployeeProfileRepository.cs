using Proposly.Domain.HrManagement.Entities;

namespace Proposly.Domain.HrManagement.Repositories;

public interface IEmployeeProfileRepository
{
    /// <summary>The profile with documents, allowances and notes loaded.</summary>
    Task<EmployeeProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>All profiles of the company, children included (team lists are small).</summary>
    Task<IReadOnlyList<EmployeeProfile>> GetAllAsync(CancellationToken ct = default);

    Task AddAsync(EmployeeProfile profile, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
