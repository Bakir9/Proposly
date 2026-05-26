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

    public async Task AddAsync(Company company, CancellationToken ct = default)
    {
        await _context.Companies.AddAsync(company, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Company company, CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);

    public async Task<IReadOnlyList<(Company Company, int UserCount, int ProjectCount)>> GetAllWithCountsAsync(CancellationToken ct = default)
    {
        var companies = await _context.Companies
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        var userCounts = await _context.Users
            .IgnoreQueryFilters()
            .Where(u => !u.IsDisabled && u.InviteToken == null && u.CompanyId != Guid.Empty)
            .GroupBy(u => u.CompanyId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var projectCounts = await _context.Projects
            .IgnoreQueryFilters()
            .GroupBy(p => p.CompanyId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var uMap = userCounts.ToDictionary(x => x.Key, x => x.Count);
        var pMap = projectCounts.ToDictionary(x => x.Key, x => x.Count);

        return companies
            .Select(c => (c, uMap.GetValueOrDefault(c.Id, 0), pMap.GetValueOrDefault(c.Id, 0)))
            .ToList();
    }
}
