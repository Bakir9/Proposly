using Microsoft.EntityFrameworkCore;
using Proposly.Domain.CalendarManagement.Entities;
using Proposly.Domain.CalendarManagement.Enums;
using Proposly.Domain.CalendarManagement.Repositories;

namespace Proposly.Infrastructure.Persistence.Repositories;

public sealed class TerminRepository : ITerminRepository
{
    private readonly AppDbContext _context;

    public TerminRepository(AppDbContext context) => _context = context;

    public async Task<Termin?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Termins
            .Include(t => t.Invitations)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<Termin>> GetForUserAsync(Guid userId, DateTime start, DateTime end, CancellationToken ct = default)
        => await _context.Termins
            .Include(t => t.Invitations)
            .Where(t => t.Start < end && t.End > start)
            .Where(t => t.OrganizerId == userId || t.Invitations.Any(i => i.InviteeId == userId))
            .OrderBy(t => t.Start)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Termin>> GetAvailabilityAsync(Guid userId, DateTime start, DateTime end, CancellationToken ct = default)
        => await _context.Termins
            .Include(t => t.Invitations)
            .Where(t => t.Start < end && t.End > start)
            .Where(t => t.Status == TerminStatus.Scheduled)
            .Where(t => t.OrganizerId == userId ||
                        t.Invitations.Any(i => i.InviteeId == userId && i.Status != InvitationStatus.Declined))
            .OrderBy(t => t.Start)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Termin>> GetPendingInvitationsAsync(Guid userId, CancellationToken ct = default)
        => await _context.Termins
            .Include(t => t.Invitations)
            .Where(t => t.Status == TerminStatus.Scheduled)
            .Where(t => t.Start >= DateTime.UtcNow)
            .Where(t => t.Invitations.Any(i => i.InviteeId == userId &&
                (i.Status == InvitationStatus.Pending || i.Status == InvitationStatus.RescheduleProposed)))
            .OrderBy(t => t.Start)
            .ToListAsync(ct);

    public async Task AddAsync(Termin termin, CancellationToken ct = default)
    {
        await _context.Termins.AddAsync(termin, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Termin termin, CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);

    public async Task DeleteAsync(Termin termin, CancellationToken ct = default)
    {
        _context.Termins.Remove(termin);
        await _context.SaveChangesAsync(ct);
    }
}
