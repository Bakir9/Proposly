using Microsoft.EntityFrameworkCore;
using Proposly.Domain.ProjectManagement.Entities;
using Proposly.Domain.ProjectManagement.Repositories;

namespace Proposly.Infrastructure.Persistence.Repositories;

public sealed class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _context;

    public ProjectRepository(AppDbContext context) => _context = context;

    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Projects
            .Include(p => p.Members)
            .Include(p => p.Tasks)
            .Include(p => p.Milestones)
            .Include(p => p.Expenses)
            .Include(p => p.TimeEntries)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken ct = default)
        => await _context.Projects
            .Include(p => p.Members)
            .OrderByDescending(p => p.CreatedAt)
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
}
