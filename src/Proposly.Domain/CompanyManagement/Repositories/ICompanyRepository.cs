using Proposly.Domain.CompanyManagement.Entities;

namespace Proposly.Domain.CompanyManagement.Repositories;

public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
