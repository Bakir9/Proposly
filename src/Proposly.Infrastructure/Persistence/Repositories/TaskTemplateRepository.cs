using Microsoft.EntityFrameworkCore;
using Proposly.Domain.ProjectManagement.Entities;
using Proposly.Domain.ProjectManagement.Repositories;

namespace Proposly.Infrastructure.Persistence.Repositories;

public sealed class TaskTemplateRepository : ITaskTemplateRepository
{
    private readonly AppDbContext _context;

    public TaskTemplateRepository(AppDbContext context) => _context = context;

    public async Task<TaskTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.TaskTemplates
            .Include(t => t.Steps)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<TaskTemplate>> GetAllAsync(CancellationToken ct = default)
        => await _context.TaskTemplates
            .Include(t => t.Steps)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

    public async Task AddAsync(TaskTemplate template, CancellationToken ct = default)
    {
        await _context.TaskTemplates.AddAsync(template, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(TaskTemplate template, CancellationToken ct = default)
    {
        _context.TaskTemplates.Remove(template);
        await _context.SaveChangesAsync(ct);
    }
}
