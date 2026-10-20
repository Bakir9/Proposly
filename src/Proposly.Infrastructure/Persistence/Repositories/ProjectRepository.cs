using Microsoft.EntityFrameworkCore;
using Proposly.Application.Abstractions;
using Proposly.Domain.ProjectManagement.Entities;
using Proposly.Domain.ProjectManagement.Repositories;

namespace Proposly.Infrastructure.Persistence.Repositories;

/// <summary>
/// Project reads are membership-scoped: a Member sees only projects where they appear in
/// ProjectMembers, while Owner/Admin (CanViewAllEmployees) see every company project. The
/// predicate lives on top of the tenant filter — never via IgnoreQueryFilters(). Outside an
/// HTTP request CanViewAllEmployees is false, so background work sees only what a member would.
/// </summary>
public sealed class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ProjectRepository(AppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    private IQueryable<Project> Visible()
        => _context.Projects.Where(p =>
            _currentUser.CanViewAllEmployees
            || p.Members.Any(m => m.UserId == _currentUser.UserId));

    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await Visible()
            .Include(p => p.Members)
            .Include(p => p.Tasks).ThenInclude(t => t.Comments)
            .Include(p => p.Tasks).ThenInclude(t => t.BlockedBy)
            .Include(p => p.Milestones)
            .Include(p => p.Expenses)
            .Include(p => p.TimeEntries)
            .Include(p => p.Notes)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Project?> GetByIdWithTimeEntriesAsync(Guid id, CancellationToken ct = default)
        => await Visible()
            .Include(p => p.TimeEntries)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Project?> GetByIdForWriteAsync(Guid id, CancellationToken ct = default)
        => await Visible()
            .Include(p => p.Members)
            .Include(p => p.Tasks).ThenInclude(t => t.Comments)
            .Include(p => p.Tasks).ThenInclude(t => t.BlockedBy)
            .Include(p => p.Milestones)
            .Include(p => p.Expenses)
            .Include(p => p.Notes)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken ct = default)
        => await Visible()
            .Include(p => p.Members)
            .Include(p => p.TimeEntries)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Project>> GetAllWithExpensesAsync(CancellationToken ct = default)
        => await Visible()
            .Include(p => p.TimeEntries)
            .Include(p => p.Expenses)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Project>> SearchAsync(string term, CancellationToken ct = default)
        => await Visible()
            .Where(p => EF.Functions.ILike(p.Name, $"%{term}%") ||
                        EF.Functions.ILike(p.ClientName, $"%{term}%") ||
                        (p.Description != null && EF.Functions.ILike(p.Description, $"%{term}%")))
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .ToListAsync(ct);

    public async Task AddAsync(Project project, CancellationToken ct = default)
    {
        await _context.Projects.AddAsync(project, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Project project, CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }

    public async Task<int> CountByCompanyIdAsync(Guid companyId, CancellationToken ct = default)
        => await _context.Projects
            .Where(p => p.CompanyId == companyId)
            .CountAsync(ct);
}
