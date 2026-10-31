using Microsoft.EntityFrameworkCore;
using Proposly.Application.Dashboard.Services;
using Proposly.Domain.WorkTimeManagement.Enums;

namespace Proposly.Infrastructure.Persistence.Repositories;

/// <summary>
/// Read seam for the dashboard's "Who's out" card (same pattern as ProjectBookingReader).
///
/// AbsenceRequest and EmploymentTerms carry the composed tenant + user filter, which is right
/// for every other read — but presence ("is a colleague absent today?") is company-visible by
/// design. This reader therefore bypasses the query filters and REINSTATES THE COMPANY FILTER
/// EXPLICITLY on every query, so tenant isolation is never weakened. It projects only absence
/// existence (user, type, dates) and the working-day pattern: the absence Reason, sick-note
/// details, rates and balances are never selected. Type redaction for non-privileged viewers
/// happens in the query handler.
/// </summary>
public sealed class WhosOutReader : IWhosOutReader
{
    private readonly AppDbContext _context;

    public WhosOutReader(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<AbsenceSlice>> GetApprovedAbsencesAsync(
        Guid companyId, DateOnly from, DateOnly to, CancellationToken ct = default)
        => await _context.AbsenceRequests
            .IgnoreQueryFilters()
            .Where(a => a.CompanyId == companyId
                        && a.Status == AbsenceStatus.Approved
                        && a.StartDate <= to
                        && a.EndDate >= from)
            .Select(a => new AbsenceSlice(a.UserId, a.Type, a.StartDate, a.EndDate))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<WorkPatternSlice>> GetWorkPatternsAsync(
        Guid companyId, CancellationToken ct = default)
        => await _context.EmploymentTerms
            .IgnoreQueryFilters()
            .Where(t => t.CompanyId == companyId)
            .GroupBy(t => t.UserId)
            .Select(g => g.OrderByDescending(t => t.ValidFrom)
                .Select(t => new WorkPatternSlice(t.UserId, t.WorkingDays))
                .First())
            .ToListAsync(ct);
}
