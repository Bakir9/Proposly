using Microsoft.EntityFrameworkCore;
using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Infrastructure.Persistence.Repositories;

/// <summary>
/// EmploymentTerms is user-owned, so the query filter already limits a member to their own
/// versions and opens the set up for an approver. Nothing here calls IgnoreQueryFilters().
/// </summary>
public sealed class EmploymentTermsRepository : IEmploymentTermsRepository
{
    private readonly AppDbContext _context;

    public EmploymentTermsRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<EmploymentTerms>> GetHistoryAsync(
        Guid userId, CancellationToken ct = default)
        => await _context.EmploymentTerms
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.ValidFrom)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<EmploymentTerms>> GetForRangeAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default)
        => await _context.EmploymentTerms
            .Where(t => t.UserId == userId
                     && t.ValidFrom <= to
                     && (t.ValidTo == null || t.ValidTo >= from))
            .OrderBy(t => t.ValidFrom)
            .ToListAsync(ct);

    public async Task<EmploymentTerms?> GetLatestAsync(Guid userId, CancellationToken ct = default)
        => await _context.EmploymentTerms
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.ValidFrom)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(EmploymentTerms terms, CancellationToken ct = default)
    {
        await _context.EmploymentTerms.AddAsync(terms, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
