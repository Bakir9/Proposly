using Proposly.Domain.CompanyManagement.Entities;

namespace Proposly.Domain.CompanyManagement.Repositories;

public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Company company, CancellationToken ct = default);
    Task UpdateAsync(Company company, CancellationToken ct = default);
    Task<IReadOnlyList<(Company Company, int UserCount, int ProjectCount)>> GetAllWithCountsAsync(CancellationToken ct = default);
}
