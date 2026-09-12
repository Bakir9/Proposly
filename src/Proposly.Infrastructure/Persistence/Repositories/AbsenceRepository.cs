using Microsoft.EntityFrameworkCore;
using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Infrastructure.Persistence.Repositories;

/// <summary>
/// Per-employee scoping comes from the AppDbContext query filter, which also keeps the company
/// filter in place. Nothing here calls IgnoreQueryFilters().
/// </summary>
public sealed class AbsenceRepository : IAbsenceRepository
{
    private readonly AppDbContext _context;

    public AbsenceRepository(AppDbContext context) => _context = context;

    public async Task<AbsenceRequest?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.AbsenceRequests.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<AbsenceRequest>> GetForYearAsync(
        int year, AbsenceStatus? status = null, CancellationToken ct = default)
    {
        var from = new DateOnly(year, 1, 1);
        var to = new DateOnly(year, 12, 31);

        var query = _context.AbsenceRequests
            .Where(a => a.StartDate <= to && a.EndDate >= from);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        return await query
            .OrderByDescending(a => a.StartDate)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AbsenceRequest>> GetPendingAsync(CancellationToken ct = default)
        => await _context.AbsenceRequests
            .Where(a => a.Status == AbsenceStatus.Pending)
            .OrderBy(a => a.StartDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AbsenceRequest>> GetOverlappingAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default)
        => await _context.AbsenceRequests
            .Where(a => a.UserId == userId
                     && (a.Status == AbsenceStatus.Pending || a.Status == AbsenceStatus.Approved)
                     && a.StartDate <= to
                     && a.EndDate >= from)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AbsenceRequest>> GetApprovedInRangeAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default)
        => await _context.AbsenceRequests
            .Where(a => a.UserId == userId
                     && a.Status == AbsenceStatus.Approved
                     && a.StartDate <= to
                     && a.EndDate >= from)
            .OrderBy(a => a.StartDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AbsenceRequest>> GetApprovedCoveringDateAsync(
        DateOnly date, CancellationToken ct = default)
        => await _context.AbsenceRequests
            .Where(a => a.Status == AbsenceStatus.Approved
                     && a.StartDate <= date
                     && a.EndDate >= date)
            .ToListAsync(ct);

    public async Task AddAsync(AbsenceRequest request, CancellationToken ct = default)
    {
        await _context.AbsenceRequests.AddAsync(request, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<AbsenceEntitlement?> GetEntitlementAsync(
        Guid userId, int year, CancellationToken ct = default)
        => await _context.AbsenceEntitlements
            .FirstOrDefaultAsync(e => e.UserId == userId && e.Year == year, ct);

    public async Task AddEntitlementAsync(
        AbsenceEntitlement entitlement, CancellationToken ct = default)
    {
        await _context.AbsenceEntitlements.AddAsync(entitlement, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
