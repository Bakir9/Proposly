using Microsoft.EntityFrameworkCore;
using Proposly.Application.WorkTimeManagement.Services;

namespace Proposly.Infrastructure.Persistence.Repositories;

/// <summary>
/// Reads project time logs for the reconciliation view. Strictly read-only: nothing here writes to
/// a TimeEntry, a Project, or any profitability figure.
/// </summary>
public sealed class ProjectBookingReader : IProjectBookingReader
{
    private readonly AppDbContext _context;

    public ProjectBookingReader(AppDbContext context) => _context = context;

    public async Task<ProjectBookingSummary> GetBookedHoursAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        // Project is tenant-filtered, so this stays inside the caller's company without any
        // explicit company predicate.
        var entries = await _context.Projects
            .SelectMany(p => p.TimeEntries.Select(e => new
            {
                p.Id,
                ProjectName = p.Name,
                e.MemberId,
                e.HoursWorked,
                e.Date
            }))
            .Where(e => e.Date >= from && e.Date <= to)
            .ToListAsync(ct);

        if (entries.Count == 0) return new ProjectBookingSummary([], 0m);

        // TimeEntry.MemberId points at ProjectMember.Id, not User.Id, so the mapping has to be
        // resolved separately. One person can hold several memberships across projects.
        var memberIds = entries.Select(e => e.MemberId).Distinct().ToList();

        var memberToUser = await _context.Projects
            .SelectMany(p => p.Members.Select(m => new { m.Id, m.UserId }))
            .Where(m => memberIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.UserId, ct);

        var mine = entries
            .Where(e => memberToUser.TryGetValue(e.MemberId, out var owner) && owner == userId)
            .ToList();

        // Memberships that no longer resolve to anyone — surfaced so the totals still reconcile
        // rather than quietly shrinking.
        var unattributed = entries
            .Where(e => !memberToUser.ContainsKey(e.MemberId))
            .Sum(e => e.HoursWorked);

        var byProject = mine
            .GroupBy(e => new { e.Id, e.ProjectName })
            .Select(g => new ProjectBookedHours(
                g.Key.Id,
                g.Key.ProjectName,
                Math.Round(g.Sum(e => e.HoursWorked), 2, MidpointRounding.AwayFromZero)))
            .OrderByDescending(p => p.BookedHours)
            .ToList();

        return new ProjectBookingSummary(
            byProject,
            Math.Round(unattributed, 2, MidpointRounding.AwayFromZero));
    }
}
