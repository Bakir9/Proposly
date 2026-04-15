using Microsoft.EntityFrameworkCore;
using Proposly.Domain.CompanyManagement.Entities;
using Proposly.Domain.CompanyManagement.Repositories;

namespace Proposly.Infrastructure.Persistence.Repositories;

public sealed class CompanyRepository : ICompanyRepository
{
    private readonly AppDbContext _context;

    public CompanyRepository(AppDbContext context) => _context = context;

    public async Task<Company?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Companies.FirstOrDefaultAsync(c => c.Id == id, ct);
}
