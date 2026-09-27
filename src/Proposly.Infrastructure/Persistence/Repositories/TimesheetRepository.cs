using Microsoft.EntityFrameworkCore;
using Proposly.Domain.WorkTimeManagement.Entities;
using Proposly.Domain.WorkTimeManagement.Enums;
using Proposly.Domain.WorkTimeManagement.Repositories;

namespace Proposly.Infrastructure.Persistence.Repositories;

/// <summary>
/// Per-employee scoping comes from the AppDbContext query filter, which also keeps the company
/// filter in place. Nothing here calls IgnoreQueryFilters() — that would drop tenant isolation
/// along with the user scope.
/// </summary>
public sealed class TimesheetRepository : ITimesheetRepository
{
    private readonly AppDbContext _context;

    public TimesheetRepository(AppDbContext context) => _context = context;

    public async Task<Timesheet?> GetForUserAsync(Guid userId, int year, int month, CancellationToken ct = default)
        => await _context.Timesheets
            .Include(t => t.Days)
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Year == year && t.Month == month, ct);

    public async Task<Timesheet?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Timesheets
            .Include(t => t.Days)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<Timesheet>> GetForMonthAsync(int year, int month, CancellationToken ct = default)
        => await _context.Timesheets
            .Include(t => t.Days)
            .Where(t => t.Year == year && t.Month == month)
            .OrderBy(t => t.UserId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Timesheet>> GetSubmittedAsync(CancellationToken ct = default)
        => await _context.Timesheets
            .Include(t => t.Days)
            .Where(t => t.Status == TimesheetStatus.Submitted)
            .OrderBy(t => t.Year).ThenBy(t => t.Month)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<WorkDayEntry>> GetDaysInRangeAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        // Narrow to the timesheets that could hold the range before flattening their days, so the
        // query stays indexed on (CompanyId, UserId, Year, Month).
        var fromKey = from.Year * 100 + from.Month;
        var toKey = to.Year * 100 + to.Month;

        return await _context.Timesheets
            .Where(t => t.UserId == userId
                     && t.Year * 100 + t.Month >= fromKey
                     && t.Year * 100 + t.Month <= toKey)
            .SelectMany(t => t.Days)
            .Where(d => d.Date >= from && d.Date <= to)
            .OrderBy(d => d.Date)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Timesheet timesheet, CancellationToken ct = default)
    {
        await _context.Timesheets.AddAsync(timesheet, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Timesheet timesheet, CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
