using Microsoft.EntityFrameworkCore;
using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Infrastructure.Persistence.Repositories;

/// <summary>
/// Company reference data, scoped by the tenant filter only — every employee reads the calendar
/// that shapes their own target hours.
/// </summary>
public sealed class NonWorkingDayRepository : INonWorkingDayRepository
{
    private readonly AppDbContext _context;

    public NonWorkingDayRepository(AppDbContext context) => _context = context;

    public async Task<NonWorkingDay?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.NonWorkingDays.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<IReadOnlyList<NonWorkingDay>> GetForRangeAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default)
        => await _context.NonWorkingDays
            .Where(d => d.Date >= from && d.Date <= to)
            .OrderBy(d => d.Date)
            .ToListAsync(ct);

    public async Task<NonWorkingDay?> GetByDateAsync(DateOnly date, CancellationToken ct = default)
        => await _context.NonWorkingDays.FirstOrDefaultAsync(d => d.Date == date, ct);

    public async Task AddAsync(NonWorkingDay day, CancellationToken ct = default)
    {
        await _context.NonWorkingDays.AddAsync(day, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(NonWorkingDay day, CancellationToken ct = default)
    {
        _context.NonWorkingDays.Remove(day);
        await _context.SaveChangesAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
