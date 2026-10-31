using Microsoft.EntityFrameworkCore;
using Proposly.Domain.ProjectManagement.Entities;
using Proposly.Domain.ProjectManagement.Repositories;

namespace Proposly.Infrastructure.Persistence.Repositories;

public sealed class ProjectFileRepository : IProjectFileRepository
{
    private readonly AppDbContext _context;

    public ProjectFileRepository(AppDbContext context) => _context = context;

    public async Task<ProjectFile?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.ProjectFiles.FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<IReadOnlyList<ProjectFile>> GetByProjectIdAsync(Guid projectId, CancellationToken ct = default)
        => await _context.ProjectFiles
            .Where(f => f.ProjectId == projectId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(ProjectFile file, CancellationToken ct = default)
    {
        await _context.ProjectFiles.AddAsync(file, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(ProjectFile file, CancellationToken ct = default)
    {
        _context.ProjectFiles.Remove(file);
        await _context.SaveChangesAsync(ct);
    }
}
