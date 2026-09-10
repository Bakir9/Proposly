using Microsoft.EntityFrameworkCore;
using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Infrastructure.Persistence.Repositories;

public sealed class WorkTimePolicyRepository : IWorkTimePolicyRepository
{
    private readonly AppDbContext _context;

    public WorkTimePolicyRepository(AppDbContext context) => _context = context;

    public async Task<WorkTimePolicy?> GetEffectiveAsync(DateOnly onDate, CancellationToken ct = default)
        => await _context.WorkTimePolicies
            .Include(p => p.BreakRules)
            .Where(p => p.ValidFrom <= onDate && (p.ValidTo == null || p.ValidTo >= onDate))
            .OrderByDescending(p => p.ValidFrom)
            .FirstOrDefaultAsync(ct);

    public async Task<WorkTimePolicy?> GetLatestAsync(CancellationToken ct = default)
        => await _context.WorkTimePolicies
            .Include(p => p.BreakRules)
            .OrderByDescending(p => p.ValidFrom)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<WorkTimePolicy>> GetHistoryAsync(CancellationToken ct = default)
        => await _context.WorkTimePolicies
            .Include(p => p.BreakRules)
            .OrderByDescending(p => p.ValidFrom)
            .ToListAsync(ct);

    public async Task AddAsync(WorkTimePolicy policy, CancellationToken ct = default)
    {
        await _context.WorkTimePolicies.AddAsync(policy, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(WorkTimePolicy policy, CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
